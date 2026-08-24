using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.StudyPlans;

public sealed class GenerateWeeklyStudyPlanCommandHandler(
    IAppDbContext dbContext,
    IStudyPlanAiClient aiClient)
    : ICommandHandler<GenerateWeeklyStudyPlanCommand, GenerateWeeklyStudyPlanResult>
{
    public async Task<GenerateWeeklyStudyPlanResult> Handle(
        GenerateWeeklyStudyPlanCommand command,
        CancellationToken cancellationToken)
    {
        StudyPlanAccess.ValidateRange(command.FromDate, command.ToDate);
        await StudyPlanAccess.EnsureTeacherOwnsCourseAsync(
            dbContext, command.TeacherId, command.CourseId, cancellationToken);

        var course = await dbContext.Courses
            .AsNoTracking()
            .Include(x => x.Stage)
            .Include(x => x.Units)
                .ThenInclude(x => x.Lessons)
            .Include(x => x.Lessons)
            .FirstOrDefaultAsync(x => x.Id == command.CourseId, cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");

        var grade = course.Grade is int gradeId
            ? await dbContext.Grades.AsNoTracking().FirstOrDefaultAsync(x => x.Id == gradeId, cancellationToken)
            : null;
        var stage = course.Stage
            ?? (course.StageId is int stageId
                ? await dbContext.Stages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == stageId, cancellationToken)
                : null);

        var weeks = StudyPlanAccess.BuildSchoolWeeks(command.FromDate, command.ToDate);
        var arabic = IsArabic(command.Language);
        DraftPlan? draft = null;
        try
        {
            var json = await aiClient.CompleteJsonAsync(
                BuildSystemPrompt(arabic),
                BuildUserPrompt(course, grade, stage, weeks, arabic),
                cancellationToken);
            draft = ParseDraft(json);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch
        {
            draft = null;
        }

        if (draft?.Weeks is null || draft.Weeks.Count == 0)
        {
            draft = BuildFallbackDraft(course, grade, weeks.Count, arabic);
        }
        else
        {
            draft = ReplaceGenericTopics(draft, course, grade, weeks.Count, arabic);
        }

        return MapToResult(weeks, draft, arabic);
    }

    private static bool IsArabic(string? language)
    {
        var value = (language ?? string.Empty).Trim().ToLowerInvariant();
        return value.StartsWith("ar") || value.Contains("arab", StringComparison.Ordinal);
    }

    private static string BuildSystemPrompt(bool arabic) =>
        arabic
            ? """
              أنت منسق مناهج وفق وزارة التربية والتعليم المصرية.
              وزّع الخطة الدراسية حسب المادة المختارة والصف الدراسي فقط، باستخدام أسماء الوحدات والدروس الفعلية.
              أرجع JSON فقط بهذا الشكل:
              {"notes":"ملاحظة قصيرة","weeks":[{"weekNumber":1,"topics":[{"title":"موضوع","highlight":false}]}]}
              قواعد:
              - weekNumber يبدأ من 1، موضوع واحد فقط لكل أسبوع في title.
              - استخدم فقط المادة المختارة والصف الدراسي والوحدات والدروس المعطاة.
              - انسخ اسم الوحدة واسم الدرس حرفياً بهذا الشكل: اسم الوحدة — اسم الدرس
              - مثال صحيح: القوى والحركة — الحركة المنتظمة
              - ممنوع كتابة "الوحدة 1" أو "الدرس 1" أو "Unit 1" أو "Lesson 1" بدل الاسم الفعلي.
              - حافظ على ترتيب المنهج (الوحدة ثم دروسها بالترتيب).
              - إذا زاد عدد الدروس عن الأسابيع اجمع درسين متتاليين من نفس الوحدة في أسبوع واحد مع الإبقاء على الاسمين.
              - إذا قلّ عدد الدروس أضف أسابيع مراجعة أو تقييم من نفس أسماء الوحدات فقط وحدد highlight=true لها.
              - highlight للمراجعة أو الاختبار فقط.
              """
            : """
              You are a curriculum planner for the Egyptian Ministry of Education.
              Build the weekly study plan from the selected subject and grade only, using the actual unit names and lesson names.
              Return JSON only in this shape:
              {"notes":"short note","weeks":[{"weekNumber":1,"topics":[{"title":"topic","highlight":false}]}]}
              Rules:
              - weekNumber starts at 1, exactly one topic per week in title.
              - Use only the selected subject, grade, and the given units and lessons.
              - Copy the unit name and lesson name exactly in this shape: unit name — lesson name
              - Correct example: Forces and motion — Uniform motion
              - Never write "Unit 1" or "Lesson 1" instead of the actual names.
              - Keep curriculum order (unit then its lessons).
              - If there are more lessons than weeks, combine two consecutive lessons from the same unit in one week and keep both names.
              - If there are fewer lessons than weeks, add review or assessment weeks from the same unit names only and set highlight=true.
              - highlight only a review or quiz week.
              """;

    private static string BuildUserPrompt(
        Course course,
        Grade? grade,
        Stage? stage,
        IReadOnlyList<(int WeekNumber, DateOnly FromDate, DateOnly ToDate)> weeks,
        bool arabic)
    {
        var academicYear = AcademicYearLabel(weeks[0].FromDate, weeks[^1].ToDate);
        var gradeName = GradeLabel(grade, course.Grade, arabic);
        var stageName = StageLabel(stage, course.StageId, arabic);
        var termName = TermLabel(course.Term, arabic);
        var schoolType = SchoolTypeLabel(course.SchoolType, arabic);
        var outline = CollectCurriculum(course);
        var sb = new StringBuilder();
        if (arabic)
        {
            sb.AppendLine("ولّد خطة دراسية أسبوعية وفق المنهج المصري فقط.");
            sb.AppendLine($"السنة الدراسية: {academicYear}");
            sb.AppendLine($"الصف / السنة الدراسية: {gradeName}");
            sb.AppendLine($"المرحلة: {stageName}");
            sb.AppendLine($"الفصل الدراسي: {termName}");
            sb.AppendLine($"نوع المدرسة: {schoolType}");
            sb.AppendLine($"المادة المختارة: {course.Title}");
            if (!string.IsNullOrWhiteSpace(course.Description))
            {
                sb.AppendLine($"وصف المادة: {course.Description.Trim()}");
            }

            sb.AppendLine($"عدد الأسابيع: {weeks.Count}");
            sb.AppendLine("الأسابيع:");
            foreach (var week in weeks)
            {
                sb.AppendLine($"- الأسبوع {week.WeekNumber}: {week.FromDate:yyyy-MM-dd} إلى {week.ToDate:yyyy-MM-dd}");
            }

            sb.AppendLine("أسماء الوحدات والدروس الفعلية للمادة المختارة والصف الدراسي:");
            AppendCurriculum(sb, outline, arabic);
            if (outline.Count == 0)
            {
                sb.AppendLine("لا توجد وحدات أو دروس مسجّلة. استخدم الأسماء الرسمية لوحدات ودروس هذه المادة وهذا الصف وفق وزارة التربية والتعليم المصرية. ممنوع كتابة الوحدة 1 أو الدرس 1.");
            }

            sb.AppendLine($"أنشئ خطة كاملة لـ {weeks.Count} أسبوعاً من المحتوى أعلاه فقط.");
        }
        else
        {
            sb.AppendLine("Generate a weekly study plan using the Egyptian curriculum only.");
            sb.AppendLine($"Academic year: {academicYear}");
            sb.AppendLine($"Grade / school year: {gradeName}");
            sb.AppendLine($"Stage: {stageName}");
            sb.AppendLine($"Term: {termName}");
            sb.AppendLine($"School type: {schoolType}");
            sb.AppendLine($"Selected subject: {course.Title}");
            if (!string.IsNullOrWhiteSpace(course.Description))
            {
                sb.AppendLine($"Subject description: {course.Description.Trim()}");
            }

            sb.AppendLine($"Week count: {weeks.Count}");
            sb.AppendLine("Weeks:");
            foreach (var week in weeks)
            {
                sb.AppendLine($"- Week {week.WeekNumber}: {week.FromDate:yyyy-MM-dd} to {week.ToDate:yyyy-MM-dd}");
            }

            sb.AppendLine("Actual unit names and lesson names for the selected subject and grade:");
            AppendCurriculum(sb, outline, arabic);
            if (outline.Count == 0)
            {
                sb.AppendLine("No units or lessons are stored. Use the official Egyptian Ministry of Education unit names and lesson names for this subject and grade. Never write Unit 1 or Lesson 1.");
            }

            sb.AppendLine($"Create a complete plan for {weeks.Count} weeks using only the content above.");
        }

        return sb.ToString();
    }

    private static void AppendCurriculum(StringBuilder sb, IReadOnlyList<CurriculumUnit> outline, bool arabic)
    {
        if (outline.Count == 0)
        {
            return;
        }

        foreach (var unit in outline)
        {
            var unitTitle = string.IsNullOrWhiteSpace(unit.Title)
                ? (arabic ? "دروس غير مربوطة بوحدة" : "Lessons not in a unit")
                : unit.Title.Trim();
            sb.AppendLine(arabic
                ? $"- اسم الوحدة: {unitTitle}"
                : $"- Unit name: {unitTitle}");
            if (!string.IsNullOrWhiteSpace(unit.Description) && !NamesMatch(unit.Title, unit.Description))
            {
                sb.AppendLine($"  {TrimText(unit.Description, 180)}");
            }

            foreach (var lesson in unit.Lessons)
            {
                sb.AppendLine(arabic
                    ? $"  - اسم الدرس: {lesson.Title}"
                    : $"  - Lesson name: {lesson.Title}");
                if (!string.IsNullOrWhiteSpace(lesson.Description) && !NamesMatch(lesson.Title, lesson.Description))
                {
                    sb.AppendLine($"    {TrimText(lesson.Description, 120)}");
                }
            }
        }
    }

    private static IReadOnlyList<CurriculumUnit> CollectCurriculum(Course course)
    {
        var units = new List<CurriculumUnit>();
        var assignedLessonIds = new HashSet<Guid>();
        foreach (var unit in course.Units.OrderBy(x => x.SortOrder).ThenBy(x => x.Title))
        {
            var lessons = unit.Lessons
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Title)
                .Select(lesson => new CurriculumLesson(
                    ResolveName(lesson.Title, lesson.Description),
                    lesson.Description))
                .Where(lesson => !string.IsNullOrWhiteSpace(lesson.Title))
                .ToList();
            foreach (var lesson in unit.Lessons)
            {
                assignedLessonIds.Add(lesson.Id);
            }

            if (string.IsNullOrWhiteSpace(unit.Title) && lessons.Count == 0)
            {
                continue;
            }

            units.Add(new CurriculumUnit(
                ResolveName(unit.Title, unit.Description),
                unit.Description,
                lessons));
        }

        var looseLessons = course.Lessons
            .Where(lesson => lesson.UnitId is null || !assignedLessonIds.Contains(lesson.Id))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title)
            .Select(lesson => new CurriculumLesson(
                ResolveName(lesson.Title, lesson.Description),
                lesson.Description))
            .Where(lesson => !string.IsNullOrWhiteSpace(lesson.Title))
            .ToList();
        if (looseLessons.Count > 0)
        {
            units.Add(new CurriculumUnit(string.Empty, null, looseLessons));
        }

        return units;
    }

    private static string AcademicYearLabel(DateOnly from, DateOnly to) =>
        from.Year == to.Year ? from.Year.ToString() : $"{from.Year} - {to.Year}";

    private static string GradeLabel(Grade? grade, int? gradeCode, bool arabic)
    {
        if (grade is not null)
        {
            var name = arabic ? grade.Name : FirstNonEmpty(grade.NameEn, grade.Name);
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }

        return gradeCode switch
        {
            null => arabic ? "جميع الصفوف" : "All grades",
            -1 => "KG1",
            0 => "KG2",
            _ => arabic ? $"الصف {gradeCode}" : $"Grade {gradeCode}"
        };
    }

    private static string StageLabel(Stage? stage, int? stageId, bool arabic)
    {
        if (stage is not null)
        {
            var name = arabic ? stage.Name : FirstNonEmpty(stage.NameEn, stage.Name);
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }

        return stageId switch
        {
            0 => arabic ? "رياض الأطفال" : "Kindergarten",
            1 => arabic ? "المرحلة الابتدائية" : "Primary",
            2 => arabic ? "المرحلة الإعدادية" : "Preparatory",
            3 => arabic ? "المرحلة الثانوية" : "Secondary",
            _ => arabic ? "غير محدد" : "Unspecified"
        };
    }

    private static string TermLabel(CourseTerm? term, bool arabic) =>
        term switch
        {
            CourseTerm.FirstTerm => arabic ? "الفصل الدراسي الأول" : "First term",
            CourseTerm.SecondTerm => arabic ? "الفصل الدراسي الثاني" : "Second term",
            CourseTerm.FullYear => arabic ? "العام الدراسي كامل" : "Full year",
            _ => arabic ? "غير محدد" : "Unspecified"
        };

    private static string SchoolTypeLabel(SchoolType? schoolType, bool arabic) =>
        schoolType switch
        {
            SchoolType.Arabic => arabic ? "عربي" : "Arabic",
            SchoolType.Language => arabic ? "لغات" : "Language",
            _ => arabic ? "الكل" : "All"
        };

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private static string TrimText(string? value, int maxLength)
    {
        var text = (value ?? string.Empty).Trim();
        return text.Length <= maxLength ? text : text[..maxLength];
    }

    private static DraftPlan? ParseDraft(string json)
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

            var notes = ReadString(root, "notes", "Notes");
            if (!TryGetProperty(root, out var weeksEl, "weeks", "Weeks")
                || weeksEl.ValueKind != JsonValueKind.Array)
            {
                return string.IsNullOrWhiteSpace(notes) ? null : new DraftPlan(notes, []);
            }

            var weeks = new List<DraftWeek>();
            foreach (var weekEl in weeksEl.EnumerateArray())
            {
                if (weekEl.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var weekNumber = ReadInt(weekEl, "weekNumber", "WeekNumber", "week", "Week");
                if (weekNumber <= 0)
                {
                    weekNumber = weeks.Count + 1;
                }

                weeks.Add(new DraftWeek(weekNumber, ReadTopics(weekEl)));
            }

            return weeks.Count == 0 && string.IsNullOrWhiteSpace(notes)
                ? null
                : new DraftPlan(notes, weeks);
        }
        catch (JsonException)
        {
            return null;
        }
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

    private static DraftPlan BuildFallbackDraft(Course course, Grade? grade, int weekCount, bool arabic)
    {
        var topics = new List<(string Title, bool Highlight)>();
        foreach (var unit in CollectCurriculum(course))
        {
            if (unit.Lessons.Count == 0 && !string.IsNullOrWhiteSpace(unit.Title) && !IsNumberedPlaceholder(unit.Title))
            {
                topics.Add((FormatTopic(unit.Title, null, arabic), false));
                continue;
            }

            foreach (var lesson in unit.Lessons)
            {
                topics.Add((FormatTopic(unit.Title, lesson.Title, arabic), false));
            }
        }

        if (topics.Count == 0)
        {
            var gradeName = GradeLabel(grade, course.Grade, arabic);
            var review = arabic ? "مراجعة" : "Review";
            for (var i = 1; i <= weekCount; i++)
            {
                topics.Add(($"{course.Title} — {gradeName} — {review} {i}", i == weekCount));
            }
        }

        var perWeek = Math.Max(1, (int)Math.Ceiling(topics.Count / (double)Math.Max(weekCount, 1)));
        var weeks = new List<DraftWeek>();
        var index = 0;
        for (var week = 1; week <= weekCount; week++)
        {
            var slice = new List<string>();
            var highlight = false;
            for (var i = 0; i < perWeek && index < topics.Count; i++, index++)
            {
                slice.Add(topics[index].Title);
                highlight = highlight || topics[index].Highlight;
            }

            if (slice.Count == 0 && topics.Count > 0)
            {
                var wrap = topics[(week - 1) % topics.Count];
                slice.Add(wrap.Title);
                highlight = wrap.Highlight;
            }

            weeks.Add(new DraftWeek(week, [new DraftTopic(string.Join(" — ", slice), highlight)]));
        }

        var notes = arabic
            ? "خطة مقترحة من المادة المختارة والصف الدراسي بأسماء الوحدات والدروس الفعلية. راجعها قبل الحفظ."
            : "Draft plan from the selected subject and grade using actual unit and lesson names. Review before saving.";
        return new DraftPlan(notes, weeks);
    }

    private static DraftPlan ReplaceGenericTopics(
        DraftPlan draft,
        Course course,
        Grade? grade,
        int weekCount,
        bool arabic)
    {
        var fallback = BuildFallbackDraft(course, grade, weekCount, arabic);
        var fallbackByWeek = (fallback.Weeks ?? [])
            .Where(week => week.WeekNumber > 0)
            .GroupBy(week => week.WeekNumber)
            .ToDictionary(group => group.Key, group => group.Last());

        var weeks = new List<DraftWeek>();
        var genericCount = 0;
        var total = 0;
        foreach (var week in draft.Weeks ?? [])
        {
            var topics = (week.Topics ?? []).ToList();
            total++;
            var generic = topics.Count == 0 || topics.All(topic => IsGenericTitle(topic.Title));
            if (generic)
            {
                genericCount++;
                if (fallbackByWeek.TryGetValue(week.WeekNumber, out var named))
                {
                    weeks.Add(named);
                    continue;
                }
            }

            weeks.Add(week);
        }

        return total > 0 && genericCount * 2 >= total
            ? fallback
            : new DraftPlan(draft.Notes, weeks);
    }

    private static string FormatTopic(string? unitTitle, string? lessonTitle, bool arabic)
    {
        var unit = ResolveName(unitTitle, null);
        var lesson = ResolveName(lessonTitle, null);
        if (unit.Length > 0 && lesson.Length > 0)
        {
            return $"{unit} — {lesson}";
        }

        if (lesson.Length > 0)
        {
            return lesson;
        }

        if (unit.Length > 0)
        {
            return unit;
        }

        return arabic ? "موضوع الأسبوع" : "Week topic";
    }

    private static string ResolveName(string? title, string? description)
    {
        var name = (title ?? string.Empty).Trim();
        if (name.Length > 0 && !IsNumberedPlaceholder(name))
        {
            return name;
        }

        var fallback = (description ?? string.Empty).Trim();
        if (fallback.Length > 0 && !IsNumberedPlaceholder(fallback))
        {
            return fallback;
        }

        return string.Empty;
    }

    private static bool NamesMatch(string? left, string? right) =>
        string.Equals((left ?? string.Empty).Trim(), (right ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);

    private static readonly Regex NumberedPlaceholderRegex = new(
        @"^\s*(ال)?(وحدة|وحده|درس|unit|lesson)\s*[:.\-]?\s*(\d+|الأول[ى]?|الثان[يية]|الثالث[ة]?|الرابع[ة]?|الخامس[ة]?|السادس[ة]?)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex NumberedTopicRegex = new(
        @"^((ال)?(وحدة|وحده|درس)|unit|lesson)(\s+\d+)?(\s+((ال)?(وحدة|وحده|درس)|unit|lesson)(\s+\d+)?)*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static bool IsNumberedPlaceholder(string? value) =>
        NumberedPlaceholderRegex.IsMatch(value ?? string.Empty);

    private static bool IsGenericTitle(string? title)
    {
        var text = (title ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return true;
        }

        var stripped = Regex.Replace(
            text,
            @"(الوحدة|الوحده|الدرس|unit|lesson)\s*[:：]\s*",
            "$1 ",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var normalized = Regex.Replace(stripped, @"[\s:：.\-—–_,،/|]+", " ").Trim();
        return NumberedTopicRegex.IsMatch(normalized);
    }

    private static GenerateWeeklyStudyPlanResult MapToResult(
        List<(int WeekNumber, DateOnly FromDate, DateOnly ToDate)> weeks,
        DraftPlan draft,
        bool arabic)
    {
        var byNumber = (draft.Weeks ?? [])
            .Where(x => x.WeekNumber > 0)
            .GroupBy(x => x.WeekNumber)
            .ToDictionary(g => g.Key, g => g.Last());

        var mapped = weeks.Select(week =>
        {
            byNumber.TryGetValue(week.WeekNumber, out var match);
            var topic = CombineTopic(match?.Topics, arabic ? "موضوع الأسبوع" : "Week topic");
            return new SaveWeeklyStudyPlanWeekDto(week.WeekNumber, week.FromDate, week.ToDate, [topic]);
        }).ToList();

        return new GenerateWeeklyStudyPlanResult(
            StudyPlanAccess.Clamp(draft.Notes, 1000),
            mapped);
    }

    private static SaveWeeklyStudyPlanTopicDto CombineTopic(IEnumerable<DraftTopic>? topics, string fallback)
    {
        var list = (topics ?? []).ToList();
        var titles = list
            .Select(topic => StudyPlanAccess.Clamp(topic.Title, StudyPlanAccess.TopicTitleMax).Trim())
            .Where(title => title.Length > 0)
            .ToList();
        var title = titles.Count == 0
            ? fallback
            : StudyPlanAccess.Clamp(string.Join("\n", titles), StudyPlanAccess.TopicTitleMax);
        return new SaveWeeklyStudyPlanTopicDto(title, list.Any(topic => topic.Highlight));
    }

    private static List<DraftTopic> ReadTopics(JsonElement weekEl)
    {
        if (TryGetProperty(weekEl, out var topicsEl, "topics", "Topics"))
        {
            return ReadTopicList(topicsEl);
        }

        var title = ReadString(weekEl, "title", "Title", "topic", "Topic", "content", "Content");
        return string.IsNullOrWhiteSpace(title)
            ? []
            : [new DraftTopic(title, ReadBool(weekEl, "highlight", "Highlight"))];
    }

    private static List<DraftTopic> ReadTopicList(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.String)
        {
            var title = el.GetString();
            return string.IsNullOrWhiteSpace(title) ? [] : [new DraftTopic(title, false)];
        }

        if (el.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var list = new List<DraftTopic>();
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var title = item.GetString();
                if (!string.IsNullOrWhiteSpace(title))
                {
                    list.Add(new DraftTopic(title, false));
                }

                continue;
            }

            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var text = ReadString(item, "title", "Title", "topic", "Topic", "content", "Content", "text", "Text");
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            list.Add(new DraftTopic(text, ReadBool(item, "highlight", "Highlight")));
        }

        return list;
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

    private static bool ReadBool(JsonElement el, params string[] names)
    {
        if (!TryGetProperty(el, out var value, names))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(value.GetString(), out var parsed) && parsed,
            _ => false
        };
    }

    private sealed record CurriculumUnit(string Title, string? Description, IReadOnlyList<CurriculumLesson> Lessons);
    private sealed record CurriculumLesson(string Title, string? Description);
    private sealed record DraftPlan(string? Notes, IReadOnlyList<DraftWeek>? Weeks);
    private sealed record DraftWeek(int WeekNumber, IReadOnlyList<DraftTopic>? Topics);
    private sealed record DraftTopic(string? Title, bool Highlight);
}
