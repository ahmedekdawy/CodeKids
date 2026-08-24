using System.Security.Cryptography;
using System.Text;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Infrastructure;

/// <summary>
/// Seeds Egyptian MoE subjects, units, and lessons from the official grades 1–12 catalog.
/// Course = subject for one grade (and track when applicable); CourseUnit = وحدة; Lesson = درس.
/// </summary>
public static class EgyptianCurriculumSeedData
{
    public static async Task SeedAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var catalog = EgyptianCurriculumCatalog.Load();
        await EnsureSubjectsAsync(dbContext, catalog, cancellationToken);
        await SyncCoursesAsync(dbContext, catalog, cancellationToken);
        await ApplyExternalSubjectIdsAsync(dbContext, cancellationToken);
    }

    private static async Task EnsureSubjectsAsync(
        AppDbContext dbContext,
        IReadOnlyList<CurriculumCourseSpec> catalog,
        CancellationToken cancellationToken)
    {
        var subjects = await dbContext.Subjects.ToListAsync(cancellationToken);
        var nextId = subjects.Count == 0 ? 1000 : Math.Max(1000, subjects.Max(s => s.Id) + 1);

        foreach (var spec in catalog
            .GroupBy(x => (x.SubjectCode, x.StageId))
            .Select(g => g.First())
            .OrderBy(x => x.StageId)
            .ThenBy(x => x.Title))
        {
            var match = subjects.FirstOrDefault(s =>
                    s.StageId == spec.StageId
                    && s.Code.Equals(spec.SubjectCode, StringComparison.OrdinalIgnoreCase))
                ?? subjects.FirstOrDefault(s =>
                    s.StageId == spec.StageId
                    && TitleAliasesFor(spec.SubjectCode, spec.Grade).Contains(s.Title));

            if (match is null)
            {
                match = new Subject
                {
                    Id = nextId++,
                    Title = Clip(spec.Title, 200),
                    StageId = spec.StageId
                };
                dbContext.Subjects.Add(match);
                subjects.Add(match);
            }

            match.Code = spec.SubjectCode;
            match.Category = Clip(spec.Category, 40);
            match.NameEn = Clip(spec.NameEn, 200);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SyncCoursesAsync(
        AppDbContext dbContext,
        IReadOnlyList<CurriculumCourseSpec> catalog,
        CancellationToken cancellationToken)
    {
        var courses = await dbContext.Courses
            .Where(c => c.SchoolType == SchoolType.Arabic && c.Grade >= 1 && c.Grade <= 12)
            .ToListAsync(cancellationToken);

        var used = new HashSet<Guid>();
        var sortOrder = courses.Count == 0 ? 100 : Math.Max(100, courses.Max(c => c.SortOrder) + 1);

        var ordered = catalog
            .OrderBy(x => x.Grade)
            .ThenBy(x => x.TrackCode.Length == 0 ? 0 : x.TrackCode == "science" ? 1 : 2)
            .ThenBy(x => x.Title);

        foreach (var spec in ordered)
        {
            var course = courses.FirstOrDefault(c =>
                    !used.Contains(c.Id)
                    && c.Grade == spec.Grade
                    && c.SubjectCode == spec.SubjectCode
                    && (c.TrackCode ?? "") == spec.TrackCode)
                ?? courses.FirstOrDefault(c =>
                    !used.Contains(c.Id)
                    && c.Grade == spec.Grade
                    && string.IsNullOrWhiteSpace(c.SubjectCode)
                    && string.IsNullOrWhiteSpace(c.TrackCode)
                    && TitleAliasesFor(spec.SubjectCode, spec.Grade).Contains(c.Title));

            if (course is null)
            {
                var (ageMin, ageMax) = AgesFor(spec.Grade);
                course = new Course
                {
                    Id = CurriculumGuid("course", spec.Grade, spec.SubjectCode, spec.TrackCode),
                    Grade = spec.Grade,
                    StageId = spec.StageId,
                    SchoolType = SchoolType.Arabic,
                    Term = CourseTerm.FullYear,
                    AgeMin = ageMin,
                    AgeMax = ageMax,
                    SortOrder = sortOrder++,
                    Units = [],
                    Lessons = []
                };
                dbContext.Courses.Add(course);
                courses.Add(course);
            }

            used.Add(course.Id);
            ApplyCourseMetadata(course, spec);
            await SyncUnitsAndLessonsAsync(dbContext, course, spec, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static void ApplyCourseMetadata(Course course, CurriculumCourseSpec spec)
    {
        var (ageMin, ageMax) = AgesFor(spec.Grade);
        var trackSuffix = string.IsNullOrWhiteSpace(spec.TrackName) ? "" : $" — {spec.TrackName}";
        var description = $"{spec.Title}{trackSuffix} — {GradeLabel(spec.Grade)} (منهج مصري 2025/2026)";
        if (!string.IsNullOrWhiteSpace(spec.Notes))
        {
            description = $"{description}. {spec.Notes}";
        }

        course.Title = Clip(spec.Title, 200);
        course.Theme = Clip(ThemeFor(spec.SubjectCode, spec.Title), 60);
        course.Description = Clip(description, 1000);
        course.AgeMin = ageMin;
        course.AgeMax = ageMax;
        course.Term = CourseTerm.FullYear;
        course.Grade = spec.Grade;
        course.StageId = spec.StageId;
        course.SchoolType = SchoolType.Arabic;
        course.SubjectCode = spec.SubjectCode;
        course.Category = Clip(spec.Category, 40);
        course.TrackCode = spec.TrackCode;
        course.TrackName = Clip(spec.TrackName, 80);
        course.VerificationStatus = Clip(spec.VerificationStatus, 80);
        course.SourceTocUrl = Clip(spec.SourceTocUrl, 500);
        course.Notes = Clip(spec.Notes, 1000);
        course.Variants = Clip(spec.Variants, 400);
    }

    private static async Task SyncUnitsAndLessonsAsync(
        AppDbContext dbContext,
        Course course,
        CurriculumCourseSpec spec,
        CancellationToken cancellationToken)
    {
        var lessonIds = await dbContext.Lessons
            .Where(l => l.CourseId == course.Id)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);
        if (await LessonsAreInUseAsync(dbContext, lessonIds, cancellationToken))
        {
            return;
        }

        await dbContext.Lessons.Where(l => l.CourseId == course.Id).ExecuteDeleteAsync(cancellationToken);
        await dbContext.CourseUnits.Where(u => u.CourseId == course.Id).ExecuteDeleteAsync(cancellationToken);

        var unitOrder = 1;
        foreach (var unitSpec in spec.Units)
        {
            var unitId = CurriculumGuid("unit", spec.Grade, spec.SubjectCode, spec.TrackCode, unitSpec.Term, unitOrder);
            var unit = new CourseUnit
            {
                Id = unitId,
                CourseId = course.Id,
                Title = Clip(unitSpec.Title, 300),
                Description = Clip($"{unitSpec.Title} — {GradeLabel(spec.Grade)} — الترم {unitSpec.Term}", 500),
                SortOrder = unitOrder,
                Term = unitSpec.Term,
                VerificationStatus = Clip(unitSpec.VerificationStatus, 80),
                Lessons = []
            };

            var lessonOrder = 1;
            foreach (var lessonTitle in unitSpec.Lessons)
            {
                unit.Lessons.Add(new Lesson
                {
                    Id = CurriculumGuid("lesson", spec.Grade, spec.SubjectCode, spec.TrackCode, unitSpec.Term, unitOrder, lessonOrder),
                    CourseId = course.Id,
                    UnitId = unitId,
                    Title = Clip(lessonTitle, 300),
                    Theme = course.Theme,
                    Description = Clip($"{lessonTitle} — {spec.Title} — {GradeLabel(spec.Grade)}", 500),
                    Difficulty = Math.Clamp(1 + spec.Grade / 3, 1, 5),
                    XpReward = 20 + spec.Grade * 2,
                    SortOrder = lessonOrder
                });
                lessonOrder++;
            }

            dbContext.CourseUnits.Add(unit);
            unitOrder++;
        }
    }

    private static async Task<bool> LessonsAreInUseAsync(
        AppDbContext dbContext,
        IReadOnlyList<Guid> lessonIds,
        CancellationToken cancellationToken)
    {
        if (lessonIds.Count == 0)
        {
            return false;
        }

        if (await dbContext.LessonVideos.AnyAsync(v => lessonIds.Contains(v.LessonId), cancellationToken))
        {
            return true;
        }

        if (await dbContext.BankQuestions.AnyAsync(
                q => q.LessonId != null && lessonIds.Contains(q.LessonId.Value), cancellationToken))
        {
            return true;
        }

        if (await dbContext.VideoWatchSessions.AnyAsync(
                s => s.LessonId != null && lessonIds.Contains(s.LessonId.Value), cancellationToken))
        {
            return true;
        }

        var stepIds = await dbContext.LessonSteps
            .Where(s => lessonIds.Contains(s.LessonId))
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);
        return stepIds.Count > 0
            && await dbContext.StudentProgress.AnyAsync(p => stepIds.Contains(p.StepId), cancellationToken);
    }

    private static Guid CurriculumGuid(params object[] parts)
    {
        var key = "egypt-curriculum-v2:" + string.Join("|", parts);
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(key));
        hash[6] = (byte)((hash[6] & 0x0f) | 0x40);
        hash[8] = (byte)((hash[8] & 0x3f) | 0x80);
        return new Guid(hash);
    }

    private static HashSet<string> TitleAliasesFor(string subjectCode, int grade)
    {
        var titles = new HashSet<string>(StringComparer.Ordinal)
        {
            subjectCode
        };

        switch (subjectCode)
        {
            case "arabic":
                titles.UnionWith(["اللغة العربية"]);
                break;
            case "english":
            case "first_foreign_language":
                titles.UnionWith(["اللغة الإنجليزية", "English", "اللغة الأجنبية الأولى"]);
                break;
            case "math":
                titles.UnionWith(["الرياضيات", "Mathematics"]);
                break;
            case "discover":
                titles.UnionWith(["اكتشف", "العلوم"]);
                break;
            case "science":
                if (grade >= 4)
                {
                    titles.UnionWith(["العلوم", "Science"]);
                }

                break;
            case "social_studies":
                titles.UnionWith(["الدراسات الاجتماعية"]);
                break;
            case "islamic_religion":
            case "religion":
                titles.UnionWith(["التربية الدينية", "تربية إسلامية", "التربية الدينية الإسلامية", "التربية الدينية"]);
                break;
            case "history":
                titles.UnionWith(["التاريخ"]);
                break;
            case "geography":
                titles.UnionWith(["الجغرافيا"]);
                break;
            case "physics":
                titles.UnionWith(["الفيزياء"]);
                break;
            case "chemistry":
                titles.UnionWith(["الكيمياء"]);
                break;
            case "biology":
                titles.UnionWith(["الأحياء"]);
                break;
        }

        return titles;
    }

    private static string ThemeFor(string subjectCode, string title) => subjectCode switch
    {
        "arabic" => "لغة",
        "english" or "first_foreign_language" or "second_foreign_language" => "English",
        "math" or "statistics" => "أرقام",
        "science" or "discover" or "integrated_science" => "علوم",
        "physics" => "فيزياء",
        "chemistry" => "كيمياء",
        "biology" => "أحياء",
        "social_studies" => "وطن",
        "history" => "تاريخ",
        "geography" => "جغرافيا",
        "islamic_religion" or "christian_religion" or "religion" => "قيم",
        "values" => "قيم",
        "ict" or "programming_ai" => "تقنية",
        _ => title
    };

    private static int StageFor(int grade) => grade switch
    {
        <= 0 => 0,
        <= 6 => 1,
        <= 9 => 2,
        _ => 3
    };

    private static (int Min, int Max) AgesFor(int grade) => grade switch
    {
        <= 6 => (5 + grade, 7 + grade),
        <= 9 => (11 + (grade - 6), 13 + (grade - 6)),
        _ => (14 + (grade - 10), 16 + (grade - 10))
    };

    private static string GradeLabel(int grade) => $"الصف {grade}";

    private static string Clip(string? value, int max)
    {
        var text = value ?? "";
        return text.Length <= max ? text : text[..max];
    }

    private static async Task ApplyExternalSubjectIdsAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!await dbContext.Subjects.AnyAsync(cancellationToken))
        {
            dbContext.Subjects.AddRange(SubjectSeedData.All);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var subjects = await dbContext.Subjects.AsNoTracking().ToListAsync(cancellationToken);
        var courses = await dbContext.Courses.ToListAsync(cancellationToken);

        foreach (var course in courses)
        {
            course.ExternalSubjectId = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var course in courses.OrderBy(c => c.Grade).ThenBy(c => c.Title))
        {
            var id = ResolveSubjectId(course, subjects);
            if (id is null)
            {
                continue;
            }

            course.ExternalSubjectId = id;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static int? ResolveSubjectId(Course course, IReadOnlyList<Subject> subjects)
    {
        var titles = TitleCandidates(course.Title);
        var stageId = course.StageId ?? (course.Grade is int grade ? StageFor(grade) : null);

        if (course.Grade is int mappedGrade)
        {
            foreach (var title in titles)
            {
                if (ExternalSubjectIds.TryGetValue((title, mappedGrade), out var preferred)
                    && subjects.Any(s => s.Id == preferred))
                {
                    return preferred;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(course.SubjectCode) && stageId is int codeStage)
        {
            var byCode = subjects
                .Where(s => s.Code == course.SubjectCode && s.StageId == codeStage)
                .OrderBy(s => s.Id)
                .Select(s => (int?)s.Id)
                .FirstOrDefault();
            if (byCode is not null)
            {
                return byCode;
            }
        }

        return subjects
            .Where(s => titles.Contains(s.Title) && (stageId is null || s.StageId == stageId))
            .OrderBy(s => s.Id)
            .Select(s => (int?)s.Id)
            .FirstOrDefault();
    }

    private static IReadOnlyList<string> TitleCandidates(string title)
    {
        if (TitleAliases.TryGetValue(title, out var canonical) && canonical != title)
        {
            return [title, canonical];
        }

        return [title];
    }

    private static readonly Dictionary<string, string> TitleAliases = new(StringComparer.Ordinal)
    {
        ["التربية الدينية"] = "تربية إسلامية",
        ["التربية الدينية الإسلامية"] = "تربية إسلامية"
    };

    private static readonly Dictionary<(string Title, int Grade), int> ExternalSubjectIds = new()
    {
        [("التاريخ", 11)] = 179,
        [("اللغة العربية", 8)] = 17,
        [("Science", 6)] = 82,
        [("اللغة الإنجليزية", 12)] = 8,
        [("Science", 8)] = 20,
        [("English", 2)] = 176,
        [("English", 3)] = 262,
        [("Mathematics", 5)] = 12,
        [("الجغرافيا", 11)] = 180,
        [("التاريخ", 10)] = 27,
        [("اللغة العربية", 6)] = 78,
        [("اللغة العربية", 9)] = 1,
        [("الرياضيات", 3)] = 98,
        [("اللغة الإنجليزية", 6)] = 79,
        [("اللغة العربية", 7)] = 13,
        [("الرياضيات", 9)] = 69,
        [("Science", 9)] = 10,
        [("التاريخ", 12)] = 194,
        [("الدراسات الاجتماعية", 9)] = 11,
        [("الجغرافيا", 12)] = 193,
        [("اللغة الإنجليزية", 11)] = 7,
        [("اللغة العربية", 10)] = 22,
        [("اللغة الإنجليزية", 8)] = 19,
        [("اللغة العربية", 3)] = 261,
        [("الرياضيات", 8)] = 58,
        [("Mathematics", 8)] = 60,
        [("تربية إسلامية", 4)] = 356,
        [("العلوم", 8)] = 38,
        [("Mathematics", 9)] = 21,
        [("تربية إسلامية", 5)] = 355,
        [("العلوم", 9)] = 39,
        [("اللغة العربية", 5)] = 70,
        [("اللغة الإنجليزية", 7)] = 15,
        [("اللغة الإنجليزية", 5)] = 71,
        [("الرياضيات", 5)] = 55,
        [("الرياضيات", 7)] = 57,
        [("Mathematics", 7)] = 59,
        [("العلوم", 7)] = 37,
        [("Science", 7)] = 16,
        [("الدراسات الاجتماعية", 7)] = 14,
        [("اللغة العربية", 4)] = 40,
        [("الرياضيات", 4)] = 83,
        [("Science", 5)] = 72
    };
}
