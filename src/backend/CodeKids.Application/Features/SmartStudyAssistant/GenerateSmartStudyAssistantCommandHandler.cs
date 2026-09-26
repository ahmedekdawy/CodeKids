using System.Text;
using System.Text.Json;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Courses;
using CodeKids.Application.Features.StudyPlans;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.SmartStudyAssistant;

public sealed class GenerateSmartStudyAssistantCommandHandler(
    IAppDbContext dbContext,
    IStudyPlanAiClient aiClient,
    ICourseBookTextProvider bookTextProvider,
    IUploadedContentTextExtractor uploadedContentExtractor)
    : ICommandHandler<GenerateSmartStudyAssistantCommand, SmartStudyAssistantResultDto>
{
    private const int BookTextMaxCharacters = 24000;

    private static readonly string[] ValidActions = ["Outline", "Summary", "Assignment", "Quiz"];

    public async Task<SmartStudyAssistantResultDto> Handle(
        GenerateSmartStudyAssistantCommand command,
        CancellationToken cancellationToken)
    {
        var action = ParseAction(command.Action);
        var course = await dbContext.Courses
            .AsNoTracking()
            .Include(x => x.Stage)
            .FirstOrDefaultAsync(x => x.Id == command.CourseId, cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");

        await StudyPlanAccess.EnsureTeacherOwnsCourseAsync(
            dbContext, command.TeacherId, course.Id, cancellationToken);

        var outline = await CourseOutlineResolver.ResolveAsync(dbContext, course, cancellationToken);
        var scope = ResolveScope(outline, command.UnitId, command.LessonId);
        var arabic = IsArabic(command.Language);
        var bookText = await bookTextProvider.TryGetBookTextAsync(course, BookTextMaxCharacters, cancellationToken);

        // Prefer freshly uploaded files over the stored course book.
        var attachments = ResolveAttachments(command);
        var uploadedText = ExtractUploadedText(attachments);
        if (!string.IsNullOrWhiteSpace(uploadedText))
        {
            bookText = uploadedText;
        }

        // Outline asks for a unit/lesson tree; assignment/quiz ask for graded questions.
        var wantsUnits = action == AssistantAction.Outline;
        var wantsQuestions = action is AssistantAction.Assignment or AssistantAction.Quiz;
        var schema = BuildSchema(wantsUnits, wantsQuestions);

        string markdown;
        var images = attachments
            .Where(x => IsImageMime(x.MimeType))
            .Select(x => new IStudyPlanAiClient.AiAttachment(x.MimeType, x.Data))
            .ToList();

        // If there are any attachments, instruct the AI to use ONLY the attached files.
        var useAttachmentsOnly = attachments.Count > 0;
        string userPrompt;
        if (useAttachmentsOnly)
        {
            if (!string.IsNullOrWhiteSpace(uploadedText))
            {
                userPrompt =
                    "Use ONLY the attached file content below to generate the requested output. " +
                    "Do NOT use the selected course, course outline, or any stored course materials.\n\n" +
                    $"Requested action: {action}\n" +
                    $"Language: {(arabic ? "Arabic" : "English")}\n\n" +
                    "Attached file content (use this exclusively):\n\n" +
                    uploadedText.Trim() +
                    "\n\nProduce the output following the system instructions and the JSON schema provided.";
            }
            else if (images.Count > 0)
            {
                userPrompt =
                    "Use ONLY the attached images to generate the requested output. " +
                    "Do NOT use the selected course, course outline, or any stored course materials. " +
                    "Perform OCR on images if needed and rely exclusively on their content.\n\n" +
                    $"Requested action: {action}\n" +
                    $"Language: {(arabic ? "Arabic" : "English")}\n\n" +
                    "Attached images are provided separately; use them as the only source of content.\n\n" +
                    "Produce the output following the system instructions and the JSON schema provided.";
            }
            else
            {
                userPrompt =
                    "Use ONLY the attached files to generate the requested output. " +
                    "Do NOT use the selected course, course outline, or any stored course materials.\n\n" +
                    $"Requested action: {action}\n" +
                    $"Language: {(arabic ? "Arabic" : "English")}\n\n" +
                    "Produce the output following the system instructions and the JSON schema provided.";
            }
        }
        else
        {
            userPrompt = BuildUserPrompt(action, course, outline, scope, arabic, bookText);
        }

        if (images.Count > 0)
        {
            // Images cannot be OCR'd locally; send them as inline parts to Gemini (AiImage chain).
            markdown = await aiClient.CompleteJsonWithFilesAsync(
                BuildSystemPrompt(action, arabic),
                userPrompt,
                images,
                cancellationToken,
                schema);
        }
        else
        {
            markdown = await aiClient.CompleteJsonAsync(
                BuildSystemPrompt(action, arabic),
                userPrompt,
                cancellationToken,
                schema);
        }
        var payload = ParsePayload(markdown);

        var title = Clamp(payload?.Title, 120, DefaultTitle(action, course, arabic));
        var body = (payload?.Markdown ?? string.Empty).Trim();
        if (body.Length == 0)
        {
            throw new InvalidOperationException("Could not generate content. Please try again.");
        }

        var questions = wantsQuestions && payload is not null
            ? ParseQuestions(payload.QuestionsJson)
            : [];
        var units = wantsUnits && payload is not null
            ? ParseUnits(payload.UnitsJson)
            : [];

        return new SmartStudyAssistantResultDto(action.ToString(), title, body, questions, units);
    }

    private static object BuildSchema(bool wantsUnits, bool wantsQuestions)
    {
        var properties = new Dictionary<string, object>
        {
            ["title"] = new { type = "string" },
            ["markdown"] = new { type = "string" }
        };
        var required = new List<string> { "title", "markdown" };

        if (wantsQuestions)
        {
            properties["questions"] = new
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
                    required = new[] { "prompt" }
                }
            };
            required.Add("questions");
        }

        if (wantsUnits)
        {
            properties["units"] = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        title = new { type = "string" },
                        sortOrder = new { type = "integer" },
                        lessons = new
                        {
                            type = "array",
                            items = new { type = "string" }
                        }
                    },
                    required = new[] { "title" }
                }
            };
            required.Add("units");
        }

        return new
        {
            type = "object",
            properties,
            required = required.ToArray()
        };
    }

    private static AssistantAction ParseAction(string? action) =>
        action?.Trim().ToLowerInvariant() switch
        {
            "outline" or "index" or "فهرس" => AssistantAction.Outline,
            "summary" or "تلخيص" or "ملخص" => AssistantAction.Summary,
            "assignment" or "واجب" => AssistantAction.Assignment,
            "quiz" or "كويز" => AssistantAction.Quiz,
            _ => throw new InvalidOperationException(
                "Action must be one of: Outline, Summary, Assignment, Quiz.")
        };

    private static bool IsArabic(string? language)
    {
        var value = (language ?? string.Empty).Trim().ToLowerInvariant();
        return value.StartsWith("ar") || value.Contains("arab", StringComparison.Ordinal);
    }

    private sealed record UploadedAttachment(string MimeType, byte[] Data);

    private const long MaxAttachmentBytes = 20 * 1024 * 1024;

    private static readonly string[] AllowedImageMimes =
        ["image/jpeg", "image/png"];

    private static List<SmartStudyAttachmentFile> ResolveAttachments(GenerateSmartStudyAssistantCommand command)
    {
        var result = new List<SmartStudyAttachmentFile>();
        foreach (var file in command.Attachments)
        {
            if (file.Data is not { Length: > 0 })
            {
                continue;
            }

            if (file.Data.Length > MaxAttachmentBytes)
            {
                throw new InvalidOperationException($"File '{file.FileName}' exceeds the 20 MB limit.");
            }

            var mime = (file.MimeType ?? string.Empty).Trim().ToLowerInvariant();
            if (mime is not "application/pdf" && !AllowedImageMimes.Contains(mime))
            {
                throw new InvalidOperationException($"Unsupported file type for '{file.FileName}'. Upload PDF, JPG or PNG files only.");
            }

            result.Add(new SmartStudyAttachmentFile(file.FileName, mime, file.Data));
        }

        return result;
    }

    private string ExtractUploadedText(List<SmartStudyAttachmentFile> attachments)
    {
        var text = new StringBuilder();
        foreach (var attachment in attachments.Where(x => x.MimeType == "application/pdf"))
        {
            try
            {
                using var stream = new MemoryStream(attachment.Data);
                var extracted = uploadedContentExtractor.TryExtractPdfText(stream, BookTextMaxCharacters, out _);
                if (!string.IsNullOrWhiteSpace(extracted))
                {
                    text.AppendLine(extracted);
                }
            }
            catch
            {
                // Skip unreadable PDFs; images still go to Gemini.
            }

            if (text.Length >= BookTextMaxCharacters)
            {
                break;
            }
        }

        var result = text.ToString().Trim();
        return result.Length <= BookTextMaxCharacters ? result : result[..BookTextMaxCharacters];
    }

    private static bool IsImageMime(string mimeType) =>
        AllowedImageMimes.Contains(mimeType.Trim().ToLowerInvariant());

    private sealed record CurriculumScope(IReadOnlyList<Guid> UnitIds, IReadOnlyList<Guid> LessonIds);

    private static CurriculumScope ResolveScope(
        CourseContentOutline outline,
        Guid? unitId,
        Guid? lessonId)
    {
        var requestedUnits = new List<Guid>();
        var requestedLessons = new List<Guid>();

        if (lessonId is Guid lesson && lesson != Guid.Empty)
        {
            var known = outline.Lessons.FirstOrDefault(x => x.Id == lesson)
                ?? throw new InvalidOperationException("Selected lesson must belong to the course.");
            requestedLessons.Add(lesson);
            if (known.UnitId is Guid unit && unit != Guid.Empty)
            {
                requestedUnits.Add(unit);
            }
        }
        else if (unitId is Guid unit && unit != Guid.Empty)
        {
            if (!outline.Units.Any(x => x.Id == unit))
            {
                throw new InvalidOperationException("Selected unit must belong to the course.");
            }

            requestedUnits.Add(unit);
        }

        return new CurriculumScope(requestedUnits, requestedLessons);
    }

    private static string BuildSystemPrompt(AssistantAction action, bool arabic)
    {
        var taskRule = action switch
        {
            AssistantAction.Outline => arabic
                ? "حلّل المحتوى واستخرج العناوين الرئيسية والفرعية ونظّمها في قائمة شجرية مرقمة بتنسيق Markdown."
                : "Analyze the content and extract main and sub headings, organized as a numbered tree list in Markdown.",
            AssistantAction.Summary => arabic
                ? "لخّص المحتوى في ملخص دراسي منظّم بعناوين ونقاط مهمة وتنسيق Markdown."
                : "Summarize the content into an organized study summary with headings and key points in Markdown.",
            AssistantAction.Assignment => arabic
                ? "أنشئ أسئلة تطبيقية تقيس فهم الطالب للدرس، مع الإجابات النموذجية المختصرة بعد كل سؤال."
                : "Create applied questions that measure student understanding, with brief model answers after each question.",
            _ => arabic
                ? "أنشئ 5 أسئلة اختيار من متعدد (4 خيارات لكل سؤال) بناءً على المحتوى، مع مفتاح الإجابات الصحيحة في النهاية."
                : "Create 5 multiple-choice questions (4 options each) based on the content, with an answer key at the end."
        };

        // Structured fields the backend persists when the teacher maps results to
        // units/lessons (outline) or a quiz/assignment (questions).
        var structuredRule = action switch
        {
            AssistantAction.Outline => arabic
                ? "أضف أيضاً خاصية units كمصفوفة من {title, sortOrder, lessons:[نصوص]} تمثل نفس الفهرس."
                : "Also add a units property: an array of {title, sortOrder, lessons:[strings]} mirroring the same outline.",
            AssistantAction.Assignment => arabic
                ? "أضف أيضاً خاصية questions كمصفوفة من {prompt, questionType (Choose أو TrueFalse أو ShortAnswer أو Paragraph), options:[نصوص], correctOption (A/B/C/D), correctAnswer, points}."
                : "Also add a questions property: an array of {prompt, questionType (Choose/TrueFalse/ShortAnswer/Paragraph), options:[strings], correctOption (A/B/C/D), correctAnswer, points}.",
            AssistantAction.Quiz => arabic
                ? "أضف أيضاً خاصية questions كمصفوفة من 5 أسئلة {prompt, questionType (Choose), options:[4 نصوص], correctOption (A/B/C/D), correctAnswer, points:1}."
                : "Also add a questions property: an array of 5 {prompt, questionType (Choose), options:[4 strings], correctOption (A/B/C/D), correctAnswer, points:1}.",
            _ => string.Empty
        };

        const string jsonShapeAr =
            """{"title":"عنوان قصير","markdown":"المحتوى الكامل بتنسيق Markdown"}""";
        const string jsonShapeEn =
            """{"title":"short title","markdown":"full content in Markdown"}""";

        return arabic
            ? $"""
              أنت مساعد دراسي ذكي لمعلم وفق منهج وزارة التربية والتعليم المصرية.
              {taskRule}
              أرجع JSON فقط بهذا الشكل:
              {jsonShapeAr}
              {structuredRule}
              قواعد:
              - اكتب كل المحتوى داخل خاصية markdown بتنسيق Markdown (عناوين # وقوائم وجداول عند الحاجة).
              - لا تشرح خارج JSON.
              - إذا تم توفير محتوى كتاب المادة، اعتمد عليه حصراً ولم تخرج عن المنهج المعطى.
              """
            : $"""
              You are a smart study assistant for a teacher following the Egyptian Ministry of Education curriculum.
              {taskRule}
              Return JSON only in this shape:
              {jsonShapeEn}
              {structuredRule}
              Rules:
              - Put the whole content inside the markdown property using Markdown formatting (headings, lists, tables when needed).
              - Do not write anything outside JSON.
              - When the uploaded course book content is provided, base everything on it and stay within the given curriculum.
              """;
    }

    private static string BuildUserPrompt(
        AssistantAction action,
        Course course,
        CourseContentOutline outline,
        CurriculumScope scope,
        bool arabic,
        string bookText)
    {
        var actionName = action switch
        {
            AssistantAction.Outline => arabic ? "فهرس المادة" : "subject outline",
            AssistantAction.Summary => arabic ? "تلخيص المحتوى" : "content summary",
            AssistantAction.Assignment => arabic ? "واجب منزلي" : "homework assignment",
            _ => arabic ? "كويز اختيار من متعدد" : "multiple-choice quiz"
        };

        var sb = new StringBuilder();
        if (arabic)
        {
            sb.AppendLine($"المطلوب: {actionName}.");
            sb.AppendLine($"المادة: {course.Title}");
            if (!string.IsNullOrWhiteSpace(course.Description))
            {
                sb.AppendLine($"الوصف: {course.Description.Trim()}");
            }
        }
        else
        {
            sb.AppendLine($"Requested: {actionName}.");
            sb.AppendLine($"Subject: {course.Title}");
            if (!string.IsNullOrWhiteSpace(course.Description))
            {
                sb.AppendLine($"Description: {course.Description.Trim()}");
            }
        }

        var text = (bookText ?? string.Empty).Trim();
        if (text.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine(arabic
                ? "محتوى الكتاب المرفوع للمادة (مقتطف) — اعتمد عليه حصراً:"
                : "Uploaded course book content (excerpt) — base everything on it only:");
            sb.AppendLine(text);
        }

        var unitFilter = scope.UnitIds.Count > 0 ? scope.UnitIds.ToHashSet() : null;
        var lessonFilter = scope.LessonIds.Count > 0 ? scope.LessonIds.ToHashSet() : null;
        var units = outline.Units
            .Where(unit => unitFilter is null || unitFilter.Contains(unit.Id))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title)
            .ToList();
        sb.AppendLine();
        sb.AppendLine(arabic ? "الوحدات والدروس المطلوبة فقط:" : "Use only these units and lessons:");
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

        if (!any)
        {
            sb.AppendLine(arabic
                ? "لا توجد وحدات مسجّلة. استخدم المنهج الرسمي لهذه المادة."
                : "No units are stored. Use the official curriculum for this subject.");
        }

        return sb.ToString();
    }

    private static AssistantPayload? ParsePayload(string json)
    {
        var text = (json ?? string.Empty).Trim();
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
        if (from < 0 || to <= from)
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(text[from..(to + 1)]);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var title = root.TryGetProperty("title", out var titleEl) && titleEl.ValueKind == JsonValueKind.String
                ? titleEl.GetString()
                : null;
            var markdown = root.TryGetProperty("markdown", out var mdEl) && mdEl.ValueKind == JsonValueKind.String
                ? mdEl.GetString()
                : null;
            if (string.IsNullOrWhiteSpace(markdown)
                && root.TryGetProperty("content", out var contentEl)
                && contentEl.ValueKind == JsonValueKind.String)
            {
                markdown = contentEl.GetString();
            }

            var questionsJson = root.TryGetProperty("questions", out var qEl) && qEl.ValueKind == JsonValueKind.Array
                ? qEl.Clone()
                : JsonElementExtensions.EmptyArray;
            var unitsJson = root.TryGetProperty("units", out var uEl) && uEl.ValueKind == JsonValueKind.Array
                ? uEl.Clone()
                : JsonElementExtensions.EmptyArray;

            return new AssistantPayload(title, markdown, questionsJson, unitsJson);
        }
        catch (JsonException)
        {
            return null;
        }
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

    private static string DefaultTitle(AssistantAction action, Course course, bool arabic) =>
        action switch
        {
            AssistantAction.Outline => arabic ? $"فهرس {course.Title}" : $"{course.Title} outline",
            AssistantAction.Summary => arabic ? $"ملخص {course.Title}" : $"{course.Title} summary",
            AssistantAction.Assignment => arabic ? $"واجب {course.Title}" : $"{course.Title} assignment",
            _ => arabic ? $"كويز {course.Title}" : $"{course.Title} quiz"
        };

    private enum AssistantAction
    {
        Outline = 0,
        Summary = 1,
        Assignment = 2,
        Quiz = 3
    }

    private static List<SmartStudyAssistantQuestionDto> ParseQuestions(JsonElement questionsJson)
    {
        var result = new List<SmartStudyAssistantQuestionDto>();
        if (questionsJson.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        var order = 1;
        foreach (var item in questionsJson.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var prompt = ReadStringProp(item, "prompt", "question") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(prompt))
            {
                continue;
            }

            var options = new List<string>();
            if (item.TryGetProperty("options", out var optionsEl) && optionsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var option in optionsEl.EnumerateArray())
                {
                    var text = option.ValueKind == JsonValueKind.String ? option.GetString()?.Trim() : null;
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        options.Add(text);
                    }
                }
            }

            var questionType = (ReadStringProp(item, "questionType", "type") ?? string.Empty).Trim();
            if (options.Count >= 2
                && !Enum.TryParse<BankQuestionType>(questionType, true, out _))
            {
                questionType = nameof(BankQuestionType.Choose);
            }
            else if (options.Count < 2 && questionType.Length == 0)
            {
                questionType = nameof(BankQuestionType.ShortAnswer);
            }

            var points = 1;
            if (item.TryGetProperty("points", out var pointsEl) && pointsEl.ValueKind == JsonValueKind.Number
                && pointsEl.TryGetInt32(out var parsedPoints) && parsedPoints > 0)
            {
                points = parsedPoints;
            }

            result.Add(new SmartStudyAssistantQuestionDto(
                prompt.Trim()[..Math.Min(prompt.Trim().Length, 400)],
                questionType,
                options,
                ReadStringProp(item, "correctOption", "correct") ?? string.Empty,
                ReadStringProp(item, "correctAnswer", "answer") ?? string.Empty,
                points,
                order++));
        }

        return result;
    }

    private static List<SmartStudyAssistantUnitDto> ParseUnits(JsonElement unitsJson)
    {
        var result = new List<SmartStudyAssistantUnitDto>();
        if (unitsJson.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        var order = 1;
        foreach (var item in unitsJson.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var title = ReadStringProp(item, "title") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var lessons = new List<string>();
            if (item.TryGetProperty("lessons", out var lessonsEl) && lessonsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var lesson in lessonsEl.EnumerateArray())
                {
                    var lessonTitle = lesson.ValueKind == JsonValueKind.String
                        ? lesson.GetString()?.Trim()
                        : lesson.ValueKind == JsonValueKind.Object
                            ? ReadStringProp(lesson, "title")
                            : null;
                    if (!string.IsNullOrWhiteSpace(lessonTitle))
                    {
                        lessons.Add(lessonTitle);
                    }
                }
            }

            var sortOrder = order;
            if (item.TryGetProperty("sortOrder", out var sortEl) && sortEl.ValueKind == JsonValueKind.Number
                && sortEl.TryGetInt32(out var parsedSort) && parsedSort > 0)
            {
                sortOrder = parsedSort;
            }

            result.Add(new SmartStudyAssistantUnitDto(title.Trim()[..Math.Min(title.Trim().Length, 200)], sortOrder, lessons));
            order++;
        }

        return result;
    }

    private static string? ReadStringProp(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                var text = value.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }
        }

        return null;
    }

    private sealed record AssistantPayload(string? Title, string? Markdown, JsonElement QuestionsJson, JsonElement UnitsJson);
}
