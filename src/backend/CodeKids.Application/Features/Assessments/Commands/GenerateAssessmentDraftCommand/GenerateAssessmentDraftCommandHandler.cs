using System.Text;
using System.Text.Json;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Courses;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Application.Features.StudyPlans;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Assessments;

public sealed class GenerateAssessmentDraftCommandHandler(
    IAppDbContext dbContext,
    IStudyPlanAiClient aiClient,
    ICommandHandler<CreateBankQuestionCommand, BankQuestionDto> createBankQuestion)
    : ICommandHandler<GenerateAssessmentDraftCommand, GeneratedAssessmentDraftDto>
{
    private static readonly object AssessmentSchema = new
    {
        type = "object",
        properties = new
        {
            title = new { type = "string" },
            description = new { type = "string" },
            questions = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        prompt = new { type = "string" },
                        questionType = new { type = "string" },
                        options = new { type = "array", items = new { type = "string" } },
                        correctOption = new { type = "string" },
                        correctAnswer = new { type = "string" },
                        points = new { type = "integer" }
                    },
                    required = new[] { "prompt", "questionType" }
                }
            }
        },
        required = new[] { "title", "questions" }
    };

    public async Task<GeneratedAssessmentDraftDto> Handle(
        GenerateAssessmentDraftCommand command,
        CancellationToken cancellationToken)
    {
        var kind = ParseKind(command.Kind);
        var (course, _) = await ResolveCourseAsync(command, cancellationToken);
        var scope = ResolveCurriculumScope(course, command.UnitIds, command.LessonIds);
        var count = ClampCount(kind, command.QuestionCount);
        var arabic = IsArabic(command.Language);

        Draft? draft;
        try
        {
            var json = await aiClient.CompleteJsonAsync(
                BuildSystemPrompt(kind, count, command.QuestionType, arabic),
                BuildUserPrompt(kind, course, scope, count, command.QuestionType, arabic),
                cancellationToken,
                AssessmentSchema);
            draft = ParseDraft(json);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch(Exception ex)
        {
            draft = null;
        }

        var questions = NormalizeQuestions(kind, draft?.Questions, count, command.QuestionType, arabic);
        if (questions.Count == 0)
        {
            throw new InvalidOperationException("Could not generate assessment questions.");
        }

        var title = Clamp(draft?.Title, 80, FallbackTitle(kind, course, arabic));
        var description = Clamp(draft?.Description, 240, FallbackDescription(kind, course, arabic));
        var questionIds = new List<Guid>();
        if (kind == AssessmentKind.Exam)
        {
            var order = 1;
            foreach (var question in questions)
            {
                var saved = await createBankQuestion.Handle(
                    new CreateBankQuestionCommand(
                        command.TeacherId,
                        course.Id,
                        LessonId: scope.LessonIds.Count == 1 ? scope.LessonIds[0] : null,
                        question.QuestionType,
                        question.Prompt,
                        PassageText: null,
                        OptionA: null,
                        OptionB: null,
                        OptionC: null,
                        OptionD: null,
                        question.Options,
                        BankCorrectAnswer(question),
                        question.Points,
                        order++,
                        Children: null),
                    cancellationToken);
                questionIds.Add(saved.Id);
            }
        }

        return new GeneratedAssessmentDraftDto(
            kind.ToString(),
            title,
            description,
            questions,
            questionIds);
    }

    private async Task<(Course Course, Guid? ClassroomId)> ResolveCourseAsync(
        GenerateAssessmentDraftCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ClassroomId is Guid classroomId)
        {
            var classroom = await dbContext.Classrooms
                .AsNoTracking()
                .Include(x => x.Courses)
                .FirstOrDefaultAsync(x => x.Id == classroomId, cancellationToken)
                ?? throw new InvalidOperationException("Classroom not found.");
            if (!classroom.Courses.Any(x => x.TeacherId == command.TeacherId))
            {
                throw new InvalidOperationException("Only an assigned classroom teacher can generate questions for that classroom.");
            }

            var courseId = command.CourseId
                ?? classroom.Courses.FirstOrDefault(x => x.TeacherId == command.TeacherId)?.CourseId
                ?? classroom.Courses.FirstOrDefault()?.CourseId
                ?? throw new InvalidOperationException("Classroom has no assigned course.");
            await StudyPlanAccess.EnsureTeacherOwnsCourseAsync(
                dbContext, command.TeacherId, courseId, cancellationToken);
            var course = await LoadCourseAsync(courseId, cancellationToken);
            return (course, classroom.Id);
        }

        if (command.CourseId is not Guid selectedCourseId)
        {
            throw new InvalidOperationException("Course is required.");
        }

        await StudyPlanAccess.EnsureTeacherOwnsCourseAsync(
            dbContext, command.TeacherId, selectedCourseId, cancellationToken);
        return (await LoadCourseAsync(selectedCourseId, cancellationToken), null);
    }

    private async Task<Course> LoadCourseAsync(Guid courseId, CancellationToken cancellationToken)
    {
        var course = await dbContext.Courses
            .AsNoTracking()
            .Include(x => x.Stage)
            .Include(x => x.Units)
                .ThenInclude(x => x.Lessons)
            .Include(x => x.Lessons)
            .FirstOrDefaultAsync(x => x.Id == courseId, cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");
        await CourseOutlineResolver.AttachFallbackUnitsAsync(dbContext, course, cancellationToken);
        return course;
    }

    private static AssessmentKind ParseKind(string? kind)
    {
        if (Enum.TryParse<AssessmentKind>(kind, true, out var parsed)
            && parsed is AssessmentKind.Quiz or AssessmentKind.Exam or AssessmentKind.Assignment)
        {
            return parsed;
        }

        throw new InvalidOperationException("Kind must be Quiz, Exam, or Assignment.");
    }

    private static int ClampCount(AssessmentKind kind, int? requested)
    {
        var fallback = kind switch
        {
            AssessmentKind.Exam => 6,
            AssessmentKind.Assignment => 1,
            _ => 5
        };
        var count = requested is > 0 ? requested.Value : fallback;
        return Math.Clamp(count, 1, 12);
    }

    private static bool IsArabic(string? language)
    {
        var value = (language ?? string.Empty).Trim().ToLowerInvariant();
        return value.StartsWith("ar") || value.Contains("arab", StringComparison.Ordinal);
    }

    private static string BuildSystemPrompt(AssessmentKind kind, int count, string? preferredType, bool arabic)
    {
        var typeRule = kind switch
        {
            AssessmentKind.Quiz => arabic
                ? "questionType دائماً MultipleChoice. 4 خيارات. correctOption حرف واحد A أو B أو C أو D."
                : "questionType is always MultipleChoice. Give 4 options. correctOption is a single letter A, B, C, or D.",
            AssessmentKind.Assignment => arabic
                ? $"questionType هو {(string.Equals(preferredType, "MultipleChoice", StringComparison.OrdinalIgnoreCase) ? "MultipleChoice مع 3 خيارات وcorrectOption حرف A أو B أو C" : "ShortAnswer مع correctAnswer نصاً قصيراً")}."
                : $"questionType is {(string.Equals(preferredType, "MultipleChoice", StringComparison.OrdinalIgnoreCase) ? "MultipleChoice with 3 options and correctOption A, B, or C" : "ShortAnswer with a short correctAnswer")}.",
            _ => arabic
                ? "questionType واحد من: Choose, TrueFalse, SingleChoice, MultiChoice. لـ TrueFalse خياران فقط. لـ MultiChoice اجعل correctOption مثل A,C."
                : "questionType is one of: Choose, TrueFalse, SingleChoice, MultiChoice. TrueFalse has exactly 2 options. MultiChoice correctOption looks like A,C."
        };

        const string jsonShapeAr =
            """{"title":"عنوان","description":"وصف قصير","questions":[{"prompt":"السؤال","questionType":"النوع","options":["أ","ب"],"correctOption":"A","correctAnswer":"","points":1}]}""";
        const string jsonShapeEn =
            """{"title":"title","description":"short description","questions":[{"prompt":"question","questionType":"type","options":["A text","B text"],"correctOption":"A","correctAnswer":"","points":1}]}""";

        return arabic
            ? $"""
              أنت معلم وفق منهج وزارة التربية والتعليم المصرية.
              أنشئ مسودة تقييم من المادة والصف والوحدات والدروس المعطاة فقط.
              أرجع JSON فقط بهذا الشكل:
              {jsonShapeAr}
              قواعد:
              - أنشئ بالضبط {count} أسئلة واضحة ومناسبة للصف.
              - {typeRule}
              - options نصوص الخيارات بالترتيب، والمفتاح A هو الأول.
              - لا تشرح خارج JSON. لا تضع أسئلة خارج المنهج المعطى.
              """
            : $"""
              You are a teacher following the Egyptian Ministry of Education curriculum.
              Create an assessment draft from the given subject, grade, units, and lessons only.
              Return JSON only in this shape:
              {jsonShapeEn}
              Rules:
              - Create exactly {count} clear questions at the right grade level.
              - {typeRule}
              - options are the choice texts in order; A is the first option.
              - Do not write anything outside JSON. Do not invent topics outside the given curriculum.
              """;
    }

    private static CurriculumScope ResolveCurriculumScope(
        Course course,
        IReadOnlyList<Guid>? unitIds,
        IReadOnlyList<Guid>? lessonIds)
    {
        var requestedUnits = (unitIds ?? []).Where(id => id != Guid.Empty).Distinct().ToList();
        var requestedLessons = (lessonIds ?? []).Where(id => id != Guid.Empty).Distinct().ToList();
        var courseUnits = course.Units.ToDictionary(x => x.Id);
        var courseLessons = course.Lessons
            .Concat(course.Units.SelectMany(unit => unit.Lessons))
            .GroupBy(lesson => lesson.Id)
            .Select(group => group.First())
            .ToDictionary(x => x.Id);

        foreach (var unitId in requestedUnits)
        {
            if (!courseUnits.ContainsKey(unitId))
            {
                throw new InvalidOperationException("Selected units must belong to the course.");
            }
        }

        foreach (var lessonId in requestedLessons)
        {
            if (!courseLessons.TryGetValue(lessonId, out var lesson))
            {
                throw new InvalidOperationException("Selected lessons must belong to the course.");
            }

            if (requestedUnits.Count > 0
                && lesson.UnitId is Guid lessonUnitId
                && !requestedUnits.Contains(lessonUnitId))
            {
                throw new InvalidOperationException("Selected lessons must belong to the selected units.");
            }
        }

        return new CurriculumScope(requestedUnits, requestedLessons);
    }

    private static string BuildUserPrompt(
        AssessmentKind kind,
        Course course,
        CurriculumScope scope,
        int count,
        string? preferredType,
        bool arabic)
    {
        var kindName = kind switch
        {
            AssessmentKind.Exam => arabic ? "امتحان" : "exam",
            AssessmentKind.Assignment => arabic ? "واجب" : "assignment",
            _ => arabic ? "اختبار قصير" : "quiz"
        };
        var sb = new StringBuilder();
        if (arabic)
        {
            sb.AppendLine($"ولّد {kindName} من {count} أسئلة لهذه المادة.");
            sb.AppendLine($"المادة: {course.Title}");
            if (!string.IsNullOrWhiteSpace(course.Description))
            {
                sb.AppendLine($"الوصف: {course.Description.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(preferredType) && kind == AssessmentKind.Assignment)
            {
                sb.AppendLine($"نوع السؤال المطلوب: {preferredType}");
            }
        }
        else
        {
            sb.AppendLine($"Generate a {kindName} with {count} questions for this subject.");
            sb.AppendLine($"Subject: {course.Title}");
            if (!string.IsNullOrWhiteSpace(course.Description))
            {
                sb.AppendLine($"Description: {course.Description.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(preferredType) && kind == AssessmentKind.Assignment)
            {
                sb.AppendLine($"Requested question type: {preferredType}");
            }
        }

        AppendCurriculum(sb, course, scope, arabic);
        return sb.ToString();
    }

    private static void AppendCurriculum(StringBuilder sb, Course course, CurriculumScope scope, bool arabic)
    {
        var unitFilter = scope.UnitIds.Count > 0 ? scope.UnitIds.ToHashSet() : null;
        var lessonFilter = scope.LessonIds.Count > 0 ? scope.LessonIds.ToHashSet() : null;
        var units = course.Units
            .Where(unit => unitFilter is null || unitFilter.Contains(unit.Id))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title)
            .ToList();
        var assigned = units.SelectMany(unit => unit.Lessons.Select(lesson => lesson.Id)).ToHashSet();
        if (arabic)
        {
            sb.AppendLine("الوحدات والدروس المطلوبة فقط:");
        }
        else
        {
            sb.AppendLine("Use only these units and lessons:");
        }

        var any = false;
        foreach (var unit in units)
        {
            var lessons = unit.Lessons
                .Where(lesson => lessonFilter is null || lessonFilter.Contains(lesson.Id))
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Title)
                .ToList();
            if (lessonFilter is not null && lessons.Count == 0)
            {
                continue;
            }

            any = true;
            sb.AppendLine($"- {unit.Title}");
            foreach (var lesson in lessons)
            {
                sb.AppendLine($"  - {lesson.Title}");
            }
        }

        foreach (var lesson in course.Lessons
            .Where(lesson => lesson.UnitId is null || !assigned.Contains(lesson.Id))
            .Where(lesson => lessonFilter is null || lessonFilter.Contains(lesson.Id))
            .Where(lesson => unitFilter is null || lesson.UnitId is null || unitFilter.Contains(lesson.UnitId.Value))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title))
        {
            any = true;
            sb.AppendLine($"- {lesson.Title}");
        }

        if (!any)
        {
            sb.AppendLine(arabic
                ? "لا توجد وحدات مسجّلة. استخدم المنهج الرسمي لهذه المادة وهذا الصف."
                : "No units are stored. Use the official curriculum for this subject and grade.");
        }
    }

    private static Draft? ParseDraft(string json)
    {
        var payload = ExtractJson(json);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var questions = new List<DraftQuestion>();
            if (TryGetProperty(root, out var questionsEl, "questions", "Questions")
                && questionsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in questionsEl.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var prompt = ReadString(item, "prompt", "Prompt", "question", "Question");
                    if (string.IsNullOrWhiteSpace(prompt))
                    {
                        continue;
                    }

                    questions.Add(new DraftQuestion(
                        prompt,
                        ReadString(item, "questionType", "QuestionType", "type", "Type"),
                        ReadStringList(item, "options", "Options"),
                        ReadString(item, "correctOption", "CorrectOption", "correct", "Correct"),
                        ReadString(item, "correctAnswer", "CorrectAnswer", "answer", "Answer"),
                        ReadInt(item, "points", "Points")));
                }
            }

            return new Draft(
                ReadString(root, "title", "Title"),
                ReadString(root, "description", "Description"),
                questions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static List<GeneratedAssessmentQuestionDto> NormalizeQuestions(
        AssessmentKind kind,
        IReadOnlyList<DraftQuestion>? raw,
        int count,
        string? preferredType,
        bool arabic)
    {
        var list = new List<GeneratedAssessmentQuestionDto>();
        var order = 1;
        foreach (var item in raw ?? [])
        {
            var normalized = kind switch
            {
                AssessmentKind.Quiz => NormalizeQuiz(item, order, arabic),
                AssessmentKind.Assignment => NormalizeAssignment(item, preferredType, order),
                _ => NormalizeExam(item, order, arabic)
            };
            if (normalized is null)
            {
                continue;
            }

            list.Add(normalized);
            order++;
            if (list.Count >= count)
            {
                break;
            }
        }

        return list;
    }

    private static GeneratedAssessmentQuestionDto? NormalizeQuiz(DraftQuestion item, int order, bool arabic)
    {
        var options = CleanOptions(item.Options, 4);
        if (options.Count < 2)
        {
            return null;
        }

        var key = ResolveCorrectKey(options, item.CorrectOption, item.CorrectAnswer);
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return new GeneratedAssessmentQuestionDto(
            Clamp(item.Prompt, 400, arabic ? "سؤال" : "Question"),
            "MultipleChoice",
            options,
            key,
            key,
            item.Points > 0 ? item.Points : 1,
            order);
    }

    private static GeneratedAssessmentQuestionDto? NormalizeAssignment(
        DraftQuestion item,
        string? preferredType,
        int order)
    {
        var wantMc = string.Equals(preferredType, "MultipleChoice", StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.QuestionType, "MultipleChoice", StringComparison.OrdinalIgnoreCase);
        if (wantMc)
        {
            var options = CleanOptions(item.Options, 3);
            while (options.Count < 3)
            {
                options.Add($"Option {Letter(options.Count)}");
            }

            options = options.Take(3).ToList();
            var key = ResolveCorrectKey(options, item.CorrectOption, item.CorrectAnswer) ?? "A";
            return new GeneratedAssessmentQuestionDto(
                Clamp(item.Prompt, 400, item.Prompt ?? "Question"),
                "MultipleChoice",
                options,
                key,
                key,
                item.Points > 0 ? item.Points : 1,
                order);
        }

        var answer = FirstNonEmpty(item.CorrectAnswer, item.CorrectOption, item.Options.FirstOrDefault());
        if (string.IsNullOrWhiteSpace(answer))
        {
            return null;
        }

        return new GeneratedAssessmentQuestionDto(
            Clamp(item.Prompt, 400, item.Prompt ?? "Question"),
            "ShortAnswer",
            [],
            string.Empty,
            Clamp(answer, 200, answer),
            item.Points > 0 ? item.Points : 1,
            order);
    }

    private static GeneratedAssessmentQuestionDto? NormalizeExam(DraftQuestion item, int order, bool arabic)
    {
        var type = ParseExamType(item.QuestionType);
        List<string> options;
        string key;
        if (type == BankQuestionType.TrueFalse)
        {
            options = arabic ? ["صواب", "خطأ"] : ["True", "False"];
            key = ResolveTrueFalseKey(item.CorrectOption, item.CorrectAnswer, item.Options);
        }
        else
        {
            options = CleanOptions(item.Options, type == BankQuestionType.Choose ? 4 : 4);
            if (options.Count < 2)
            {
                return null;
            }

            key = ResolveCorrectKey(options, item.CorrectOption, item.CorrectAnswer) ?? "A";
            if (type == BankQuestionType.MultiChoice)
            {
                key = NormalizeMultiKeys(options, item.CorrectOption, item.CorrectAnswer, key);
            }
        }

        return new GeneratedAssessmentQuestionDto(
            Clamp(item.Prompt, 400, arabic ? "سؤال" : "Question"),
            type.ToString(),
            options,
            key,
            key,
            item.Points > 0 ? item.Points : 1,
            order);
    }

    private static BankQuestionType ParseExamType(string? value)
    {
        if (Enum.TryParse<BankQuestionType>(value, true, out var parsed)
            && parsed is BankQuestionType.Choose or BankQuestionType.TrueFalse
                or BankQuestionType.SingleChoice or BankQuestionType.MultiChoice)
        {
            return parsed;
        }

        return BankQuestionType.SingleChoice;
    }

    private static List<string> CleanOptions(IReadOnlyList<string>? options, int max)
    {
        var list = new List<string>();
        foreach (var option in options ?? [])
        {
            var text = (option ?? string.Empty).Trim();
            if (text.Length == 0)
            {
                continue;
            }

            if (text.Length > 2 && char.IsLetter(text[0]) && (text[1] is '.' or ')' or ':' or '-'))
            {
                text = text[2..].Trim();
            }

            if (text.Length == 0 || list.Contains(text, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            list.Add(Clamp(text, 160, text));
            if (list.Count >= max)
            {
                break;
            }
        }

        return list;
    }

    private static string? ResolveCorrectKey(
        IReadOnlyList<string> options,
        string? correctOption,
        string? correctAnswer)
    {
        var raw = FirstNonEmpty(correctOption, correctAnswer);
        if (string.IsNullOrWhiteSpace(raw) || options.Count == 0)
        {
            return null;
        }

        var trimmed = raw.Trim();
        if (trimmed.Length == 1)
        {
            var index = char.ToUpperInvariant(trimmed[0]) - 'A';
            if (index >= 0 && index < options.Count)
            {
                return Letter(index);
            }
        }

        for (var i = 0; i < options.Count; i++)
        {
            if (string.Equals(options[i], trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return Letter(i);
            }
        }

        return null;
    }

    private static string ResolveTrueFalseKey(
        string? correctOption,
        string? correctAnswer,
        IReadOnlyList<string> options)
    {
        var raw = FirstNonEmpty(correctOption, correctAnswer, options.FirstOrDefault())?.Trim() ?? "A";
        if (raw.StartsWith("T", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("صواب", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("صح", StringComparison.OrdinalIgnoreCase)
            || raw is "A" or "a" or "1")
        {
            return "A";
        }

        return "B";
    }

    private static string NormalizeMultiKeys(
        IReadOnlyList<string> options,
        string? correctOption,
        string? correctAnswer,
        string fallback)
    {
        var raw = FirstNonEmpty(correctOption, correctAnswer) ?? fallback;
        var keys = new List<string>();
        foreach (var part in raw.Split([',', ';', ' ', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var key = ResolveCorrectKey(options, part, null);
            if (!string.IsNullOrWhiteSpace(key) && !keys.Contains(key, StringComparer.OrdinalIgnoreCase))
            {
                keys.Add(key);
            }
        }

        return keys.Count == 0 ? fallback : string.Join(',', keys);
    }

    private static string ExtractJson(string raw)
    {
        var text = (raw ?? string.Empty).Trim();
        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf("```", StringComparison.Ordinal);
            if (start >= 0 && end > start)
            {
                text = text[start..end].Trim();
            }
        }

        var from = text.IndexOf('{');
        var to = text.LastIndexOf('}');
        return from >= 0 && to > from ? text[from..(to + 1)] : text;
    }

    private static bool TryGetProperty(JsonElement el, out JsonElement value, params string[] names)
    {
        foreach (var name in names)
        {
            if (el.TryGetProperty(name, out value))
            {
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string? ReadString(JsonElement el, params string[] names)
    {
        return TryGetProperty(el, out var value, names) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static int ReadInt(JsonElement el, params string[] names)
    {
        if (!TryGetProperty(el, out var value, names))
        {
            return 0;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), out var parsed) => parsed,
            _ => 0
        };
    }

    private static List<string> ReadStringList(JsonElement el, params string[] names)
    {
        if (!TryGetProperty(el, out var value, names) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var list = new List<string>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var text = item.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    list.Add(text);
                }
            }
        }

        return list;
    }

    private static string Clamp(string? value, int max, string fallback)
    {
        var text = (value ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            text = fallback;
        }

        return text.Length <= max ? text : text[..max];
    }

    private static string FallbackTitle(AssessmentKind kind, Course course, bool arabic) =>
        kind switch
        {
            AssessmentKind.Exam => arabic ? $"امتحان {course.Title}" : $"{course.Title} exam",
            AssessmentKind.Assignment => arabic ? $"واجب {course.Title}" : $"{course.Title} assignment",
            _ => arabic ? $"اختبار {course.Title}" : $"{course.Title} quiz"
        };

    private static string FallbackDescription(AssessmentKind kind, Course course, bool arabic) =>
        arabic
            ? $"أسئلة من منهج {course.Title}. راجعها قبل الحفظ."
            : $"Questions from {course.Title}. Review before saving.";

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private static string BankCorrectAnswer(GeneratedAssessmentQuestionDto question)
    {
        if (string.Equals(question.QuestionType, nameof(BankQuestionType.TrueFalse), StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(question.CorrectOption, "B", StringComparison.OrdinalIgnoreCase)
                ? "False"
                : "True";
        }

        return string.IsNullOrWhiteSpace(question.CorrectOption)
            ? question.CorrectAnswer
            : question.CorrectOption;
    }

    private static string Letter(int index) => ((char)('A' + index)).ToString();

    private enum AssessmentKind
    {
        Quiz = 0,
        Exam = 1,
        Assignment = 2
    }

    private sealed record CurriculumScope(IReadOnlyList<Guid> UnitIds, IReadOnlyList<Guid> LessonIds);
    private sealed record Draft(string? Title, string? Description, IReadOnlyList<DraftQuestion> Questions);
    private sealed record DraftQuestion(
        string? Prompt,
        string? QuestionType,
        IReadOnlyList<string> Options,
        string? CorrectOption,
        string? CorrectAnswer,
        int Points);
}
