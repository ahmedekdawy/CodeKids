using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Courses;

public sealed class GenerateCourseTreeCommandHandler(
    IAppDbContext dbContext,
    IStudyPlanAiClient aiClient)
    : ICommandHandler<GenerateCourseTreeCommand, GenerateCourseTreeResult>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly object ResponseSchema = new
    {
        type = "object",
        properties = new
        {
            notes = new { type = "string" },
            units = new
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
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    title = new { type = "string" },
                                    sortOrder = new { type = "integer" }
                                },
                                required = new[] { "title", "sortOrder" }
                            }
                        }
                    },
                    required = new[] { "title", "sortOrder", "lessons" }
                }
            }
        },
        required = new[] { "notes", "units" }
    };

    public async Task<GenerateCourseTreeResult> Handle(
        GenerateCourseTreeCommand command,
        CancellationToken cancellationToken)
    {
        var mode = ParseMode(command.Mode);
        var course = await dbContext.Courses
            .Include(x => x.Stage)
            .FirstOrDefaultAsync(x => x.Id == command.CourseId, cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");

        var outline = await CourseOutlineResolver.ResolveAsync(dbContext, course, cancellationToken);
        var grade = course.Grade is int gradeId
            ? await dbContext.Grades.AsNoTracking().FirstOrDefaultAsync(x => x.Id == gradeId, cancellationToken)
            : null;
        var stage = course.Stage
            ?? (course.StageId is int stageId
                ? await dbContext.Stages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == stageId, cancellationToken)
                : null);

        var teacherPrompt = CourseOutlineResolver.Clamp(command.Prompt, CourseTreeAccess.PromptMax);
        var arabic = IsArabic(command.Language);
        Draft? draft = null;
        try
        {
            var json = await aiClient.CompleteJsonAsync(
                BuildSystemPrompt(mode, arabic),
                BuildUserPrompt(course, grade, stage, outline, mode, arabic, teacherPrompt),
                cancellationToken,
                ResponseSchema);
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

        draft ??= BuildFallbackDraft(course, outline, mode, arabic);
        var units = NormalizeUnits(draft.Units);
        if (units.Count == 0)
        {
            throw new InvalidOperationException("Could not generate course index units.");
        }

        var notes = CourseOutlineResolver.Clamp(draft.Notes, 500);
        if (command.Apply)
        {
            await ApplyDraftAsync(course, units, mode, cancellationToken);
            notes = AppendAppliedNote(notes, mode, arabic);
        }

        return new GenerateCourseTreeResult(
            notes,
            mode.ToString(),
            command.Apply,
            units);
    }

    private static CourseTreeMode ParseMode(string? mode)
    {
        var value = (mode ?? string.Empty).Trim().ToLowerInvariant();
        return value is "update" or "refresh" or "enhance" ? CourseTreeMode.Update : CourseTreeMode.Rebuild;
    }

    private static bool IsArabic(string? language)
    {
        var value = (language ?? string.Empty).Trim().ToLowerInvariant();
        return value.StartsWith("ar") || value.Contains("arab", StringComparison.Ordinal);
    }

    private static string BuildSystemPrompt(CourseTreeMode mode, bool arabic) =>
        mode == CourseTreeMode.Rebuild
            ? arabic
                ? """
                  أنت خبير مناهج وفق وزارة التربية والتعليم المصرية.
                  أعد بناء فهرس المادة (وحدات ودروس) للمادة والصف المحددين.
                  أرجع JSON فقط بهذا الشكل:
                  {"notes":"ملاحظة قصيرة","units":[{"title":"عنوان الوحدة","sortOrder":1,"lessons":[{"title":"عنوان الدرس","sortOrder":1}]}]}
                  قواعد:
                  - sortOrder يبدأ من 1 لكل وحدة ودرس.
                  - استخدم أسماء وحدات ودروس رسمية مناسبة للمادة والصف في مصر.
                  - 4 إلى 8 وحدات، و3 إلى 10 دروس لكل وحدة حسب طبيعة المادة.
                  - ممنوع أسماء عامة مثل "الوحدة 1" أو "الدرس 1" بدون عنوان حقيقي.
                  - إذا قدّم المعلم تعليمات إضافية، اتبعها ما لم تتعارض مع القواعد أعلاه.
                  """
                : """
                  You are an Egyptian Ministry of Education curriculum expert.
                  Rebuild the course index (units and lessons) for the selected subject and grade.
                  Return JSON only in this shape:
                  {"notes":"short note","units":[{"title":"Unit title","sortOrder":1,"lessons":[{"title":"Lesson title","sortOrder":1}]}]}
                  Rules:
                  - sortOrder starts at 1 for each unit and lesson.
                  - Use official-style unit and lesson names for the subject and grade in Egypt.
                  - Provide 4 to 8 units and 3 to 10 lessons per unit depending on the subject.
                  - Never use generic names like "Unit 1" or "Lesson 1" without a real title.
                  - If the teacher provides extra instructions, follow them when they do not conflict with the rules above.
                  """
            : arabic
                ? """
                  أنت خبير مناهج وفق وزارة التربية والتعليم المصرية.
                  حدّث فهرس المادة الحالي بإضافة أو تحسين الوحدات والدروس دون حذف المحتوى الحالي.
                  أرجع JSON فقط بهذا الشكل:
                  {"notes":"ملاحظة قصيرة","units":[{"title":"عنوان الوحدة","sortOrder":1,"lessons":[{"title":"عنوان الدرس","sortOrder":1}]}]}
                  قواعد:
                  - أبقِ الوحدات والدروس الموجودة بنفس العناوين قدر الإمكان.
                  - أضف وحدات أو دروس ناقصة، أو حسّن الترتيب والتسمية عند الحاجة.
                  - sortOrder يبدأ من 1.
                  - إذا قدّم المعلم تعليمات إضافية، اتبعها ما لم تتعارض مع القواعد أعلاه.
                  """
                : """
                  You are an Egyptian Ministry of Education curriculum expert.
                  Update the existing course index by adding or improving units and lessons without removing current content.
                  Return JSON only in this shape:
                  {"notes":"short note","units":[{"title":"Unit title","sortOrder":1,"lessons":[{"title":"Lesson title","sortOrder":1}]}]}
                  Rules:
                  - Keep existing units and lessons with the same titles when possible.
                  - Add missing units or lessons, or improve ordering and naming when needed.
                  - sortOrder starts at 1.
                  - If the teacher provides extra instructions, follow them when they do not conflict with the rules above.
                  """;

    private static string BuildUserPrompt(
        Course course,
        Grade? grade,
        Stage? stage,
        CourseContentOutline outline,
        CourseTreeMode mode,
        bool arabic,
        string? teacherPrompt)
    {
        var sb = new StringBuilder();
        if (arabic)
        {
            sb.AppendLine(mode == CourseTreeMode.Rebuild
                ? "أعد بناء فهرس المادة بالكامل."
                : "حدّث فهرس المادة الحالي مع الإبقاء على المحتوى الموجود.");
            sb.AppendLine($"المادة: {course.Title}");
            sb.AppendLine($"الصف: {GradeLabel(grade, course.Grade, arabic)}");
            sb.AppendLine($"المرحلة: {StageLabel(stage, course.StageId, arabic)}");
            if (!string.IsNullOrWhiteSpace(course.Description))
            {
                sb.AppendLine($"وصف المادة: {course.Description.Trim()}");
            }

            sb.AppendLine("الفهرس الحالي:");
            AppendOutline(sb, outline, arabic);
            if (outline.Units.Count == 0)
            {
                sb.AppendLine("لا يوجد فهرس حالياً. أنشئ فهرساً كاملاً مناسباً للمادة والصف.");
            }
        }
        else
        {
            sb.AppendLine(mode == CourseTreeMode.Rebuild
                ? "Rebuild the full course index."
                : "Update the current course index while keeping existing content.");
            sb.AppendLine($"Subject: {course.Title}");
            sb.AppendLine($"Grade: {GradeLabel(grade, course.Grade, arabic)}");
            sb.AppendLine($"Stage: {StageLabel(stage, course.StageId, arabic)}");
            if (!string.IsNullOrWhiteSpace(course.Description))
            {
                sb.AppendLine($"Subject description: {course.Description.Trim()}");
            }

            sb.AppendLine("Current index:");
            AppendOutline(sb, outline, arabic);
            if (outline.Units.Count == 0)
            {
                sb.AppendLine("There is no index yet. Create a complete index for this subject and grade.");
            }
        }

        AppendTeacherPrompt(sb, teacherPrompt, arabic);
        return sb.ToString();
    }

    private static void AppendTeacherPrompt(StringBuilder sb, string? teacherPrompt, bool arabic)
    {
        var text = (teacherPrompt ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return;
        }

        sb.AppendLine();
        if (arabic)
        {
            sb.AppendLine("تعليمات إضافية من المعلم:");
            sb.AppendLine(text);
        }
        else
        {
            sb.AppendLine("Additional teacher instructions:");
            sb.AppendLine(text);
        }
    }

    private static void AppendOutline(StringBuilder sb, CourseContentOutline outline, bool arabic)
    {
        if (outline.Units.Count == 0)
        {
            sb.AppendLine(arabic ? "(فارغ)" : "(empty)");
            return;
        }

        foreach (var unit in outline.Units.OrderBy(u => u.SortOrder).ThenBy(u => u.Title))
        {
            sb.AppendLine(arabic
                ? $"- الوحدة {unit.SortOrder}: {unit.Title}"
                : $"- Unit {unit.SortOrder}: {unit.Title}");
            foreach (var lesson in unit.Lessons.OrderBy(l => l.SortOrder).ThenBy(l => l.Title))
            {
                sb.AppendLine(arabic
                    ? $"  - الدرس {lesson.SortOrder}: {lesson.Title}"
                    : $"  - Lesson {lesson.SortOrder}: {lesson.Title}");
            }
        }
    }

    private static Draft? ParseDraft(string json)
    {
        var text = ExtractJson(json);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return JsonSerializer.Deserialize<Draft>(text, JsonOptions);
    }

    private static string ExtractJson(string raw)
    {
        var text = (raw ?? string.Empty).Trim();
        if (text.StartsWith('{'))
        {
            return text;
        }

        var match = Regex.Match(text, @"\{[\s\S]*\}");
        return match.Success ? match.Value : text;
    }

    private static Draft BuildFallbackDraft(
        Course course,
        CourseContentOutline outline,
        CourseTreeMode mode,
        bool arabic)
    {
        if (mode == CourseTreeMode.Update && outline.Units.Count > 0)
        {
            return new Draft
            {
                Notes = arabic
                    ? "تعذّر الاتصال بالذكاء الاصطناعي. تم الاحتفاظ بالفهرس الحالي."
                    : "AI was unavailable. The current index was kept.",
                Units = outline.Units
                    .OrderBy(u => u.SortOrder)
                    .Select(u => new DraftUnit
                    {
                        Title = u.Title,
                        SortOrder = u.SortOrder,
                        Lessons = u.Lessons
                            .OrderBy(l => l.SortOrder)
                            .Select(l => new DraftLesson { Title = l.Title, SortOrder = l.SortOrder })
                            .ToList()
                    })
                    .ToList()
            };
        }

        var unitTitle = arabic ? $"مقدمة في {course.Title}" : $"Introduction to {course.Title}";
        return new Draft
        {
            Notes = arabic
                ? "مسودة تلقائية. راجع العناوين قبل الحفظ."
                : "Automatic draft. Review titles before saving.",
            Units =
            [
                new DraftUnit
                {
                    Title = unitTitle,
                    SortOrder = 1,
                    Lessons =
                    [
                        new DraftLesson
                        {
                            Title = arabic ? "نظرة عامة" : "Overview",
                            SortOrder = 1
                        },
                        new DraftLesson
                        {
                            Title = arabic ? "المفاهيم الأساسية" : "Core concepts",
                            SortOrder = 2
                        },
                        new DraftLesson
                        {
                            Title = arabic ? "تطبيقات عملية" : "Practical applications",
                            SortOrder = 3
                        }
                    ]
                }
            ]
        };
    }

    private static IReadOnlyList<GeneratedCourseTreeUnitDto> NormalizeUnits(IReadOnlyList<DraftUnit>? units)
    {
        if (units is null || units.Count == 0)
        {
            return [];
        }

        var result = new List<GeneratedCourseTreeUnitDto>();
        var unitOrder = 1;
        foreach (var unit in units.OrderBy(u => u.SortOrder <= 0 ? 9999 : u.SortOrder).ThenBy(u => u.Title))
        {
            var title = CourseOutlineResolver.Clamp(unit.Title, 300);
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var lessons = new List<GeneratedCourseTreeLessonDto>();
            var lessonOrder = 1;
            foreach (var lesson in (unit.Lessons ?? []).OrderBy(l => l.SortOrder <= 0 ? 9999 : l.SortOrder).ThenBy(l => l.Title))
            {
                var lessonTitle = CourseOutlineResolver.Clamp(lesson.Title, 300);
                if (string.IsNullOrWhiteSpace(lessonTitle))
                {
                    continue;
                }

                lessons.Add(new GeneratedCourseTreeLessonDto(lessonTitle, lessonOrder++));
            }

            if (lessons.Count == 0)
            {
                continue;
            }

            result.Add(new GeneratedCourseTreeUnitDto(title, unitOrder++, lessons));
        }

        return result;
    }

    private async Task ApplyDraftAsync(
        Course course,
        IReadOnlyList<GeneratedCourseTreeUnitDto> units,
        CourseTreeMode mode,
        CancellationToken cancellationToken)
    {
        var subjects = await CourseOutlineResolver.LoadRelatedSubjectsAsync(dbContext, course, cancellationToken);
        if (subjects.Count == 0)
        {
            var created = await EnsureSubjectAsync(course, cancellationToken);
            subjects = [created];
        }

        if (mode == CourseTreeMode.Rebuild)
        {
            foreach (var subject in subjects)
            {
                foreach (var unit in subject.Units.ToList())
                {
                    dbContext.SubjectUnitLessons.RemoveRange(unit.Lessons);
                    dbContext.SubjectUnits.Remove(unit);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await CreateUnitsAsync(subjects[0], units, cancellationToken);
            return;
        }

        var target = ResolvePrimarySubject(course, subjects);
        var existingUnits = target.Units.ToList();
        foreach (var draftUnit in units)
        {
            var match = existingUnits.FirstOrDefault(u => TitlesMatch(u.Title, draftUnit.Title))
                ?? existingUnits.FirstOrDefault(u => u.SortOrder == draftUnit.SortOrder);
            if (match is null)
            {
                var createdUnit = new SubjectUnit
                {
                    SubjectId = target.Id,
                    Title = draftUnit.Title,
                    SortOrder = draftUnit.SortOrder
                };
                dbContext.SubjectUnits.Add(createdUnit);
                await dbContext.SaveChangesAsync(cancellationToken);
                existingUnits.Add(createdUnit);
                await AddLessonsAsync(createdUnit, draftUnit.Lessons, [], cancellationToken);
                continue;
            }

            if (!TitlesMatch(match.Title, draftUnit.Title))
            {
                match.Title = draftUnit.Title;
            }

            match.SortOrder = draftUnit.SortOrder;
            var existingLessons = match.Lessons.ToList();
            await AddLessonsAsync(match, draftUnit.Lessons, existingLessons, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Subject ResolvePrimarySubject(Course course, IReadOnlyList<Subject> subjects)
    {
        if (course.TermId is CourseTerm term)
        {
            var match = subjects.FirstOrDefault(s => s.TermId == (int)term);
            if (match is not null)
            {
                return match;
            }
        }

        return subjects[0];
    }

    private async Task<Subject> EnsureSubjectAsync(Course course, CancellationToken cancellationToken)
    {
        var code = string.IsNullOrWhiteSpace(course.SubjectCode)
            ? SlugCode(course.Title)
            : course.SubjectCode.Trim();
        var subject = new Subject
        {
            Title = CourseOutlineResolver.Clamp(course.Title, 200),
            Code = CourseOutlineResolver.Clamp(code, 80),
            Category = string.IsNullOrWhiteSpace(course.Category) ? "core" : course.Category,
            NameEn = CourseOutlineResolver.Clamp(course.Title, 200),
            StageId = course.StageId ?? 1,
            GradeId = course.Grade,
            TermId = course.TermId is CourseTerm term ? (int)term : null,
            TrackCode = course.TrackCode ?? string.Empty,
            TrackName = course.TrackName ?? string.Empty
        };
        dbContext.Subjects.Add(subject);
        await dbContext.SaveChangesAsync(cancellationToken);
        course.ExternalSubjectId = subject.Id;
        if (string.IsNullOrWhiteSpace(course.SubjectCode))
        {
            course.SubjectCode = subject.Code;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return subject;
    }

    private async Task CreateUnitsAsync(
        Subject subject,
        IReadOnlyList<GeneratedCourseTreeUnitDto> units,
        CancellationToken cancellationToken)
    {
        foreach (var draftUnit in units)
        {
            var unit = new SubjectUnit
            {
                SubjectId = subject.Id,
                Title = draftUnit.Title,
                SortOrder = draftUnit.SortOrder
            };
            dbContext.SubjectUnits.Add(unit);
            await dbContext.SaveChangesAsync(cancellationToken);
            await AddLessonsAsync(unit, draftUnit.Lessons, [], cancellationToken);
        }
    }

    private async Task AddLessonsAsync(
        SubjectUnit unit,
        IReadOnlyList<GeneratedCourseTreeLessonDto> lessons,
        IReadOnlyList<SubjectUnitLesson> existingLessons,
        CancellationToken cancellationToken)
    {
        foreach (var draftLesson in lessons)
        {
            if (existingLessons.Any(l => TitlesMatch(l.Title, draftLesson.Title)))
            {
                continue;
            }

            var lesson = new SubjectUnitLesson
            {
                SubjectUnitId = unit.Id,
                Title = draftLesson.Title,
                SortOrder = draftLesson.SortOrder
            };
            dbContext.SubjectUnitLessons.Add(lesson);
            unit.Lessons.Add(lesson);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool TitlesMatch(string? left, string? right)
    {
        var a = NormalizeTitle(left);
        var b = NormalizeTitle(right);
        return a.Length > 0 && a == b;
    }

    private static string NormalizeTitle(string? value) =>
        Regex.Replace((value ?? string.Empty).Trim().ToLowerInvariant(), @"\s+", " ");

    private static string SlugCode(string title)
    {
        var slug = Regex.Replace((title ?? "course").Trim().ToLowerInvariant(), @"[^a-z0-9\u0600-\u06FF]+", "_");
        slug = slug.Trim('_');
        return string.IsNullOrWhiteSpace(slug) ? "course" : slug[..Math.Min(slug.Length, 40)];
    }

    private static string AppendAppliedNote(string notes, CourseTreeMode mode, bool arabic)
    {
        var suffix = mode == CourseTreeMode.Rebuild
            ? arabic ? "تمت إعادة بناء الفهرس." : "Index rebuilt."
            : arabic ? "تم تحديث الفهرس." : "Index updated.";
        return string.IsNullOrWhiteSpace(notes) ? suffix : $"{notes.Trim()} {suffix}";
    }

    private static string GradeLabel(Grade? grade, int? gradeId, bool arabic)
    {
        if (grade is not null)
        {
            return arabic ? grade.Name : grade.NameEn;
        }

        return gradeId?.ToString() ?? (arabic ? "غير محدد" : "Not set");
    }

    private static string StageLabel(Stage? stage, int? stageId, bool arabic)
    {
        if (stage is not null)
        {
            return arabic ? stage.Name : stage.NameEn;
        }

        return stageId?.ToString() ?? (arabic ? "غير محدد" : "Not set");
    }

    private enum CourseTreeMode
    {
        Rebuild,
        Update
    }

    private sealed class Draft
    {
        public string Notes { get; set; } = string.Empty;
        public List<DraftUnit> Units { get; set; } = [];
    }

    private sealed class DraftUnit
    {
        public string Title { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public List<DraftLesson> Lessons { get; set; } = [];
    }

    private sealed class DraftLesson
    {
        public string Title { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }
}
