using System.Security.Cryptography;
using System.Text;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Infrastructure;

/// <summary>
/// Seeds Egyptian MoE subjects, units, and lessons from the official grades 1–12 catalog.
/// Course = subject for one grade (and track when applicable); SubjectUnit = وحدة; SubjectUnitLesson = درس.
/// </summary>
public static class EgyptianCurriculumSeedData
{
    public static async Task SeedAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var offerings = EgyptianCurriculumCatalog.LoadOfferings();
        var catalog = EgyptianCurriculumCatalog.Load();
        await EnsureSubjectsAsync(dbContext, offerings, cancellationToken);
        await SyncCoursesAsync(dbContext, catalog, cancellationToken);
        await ApplyExternalSubjectIdsAsync(dbContext, cancellationToken);
    }

    private static async Task EnsureSubjectsAsync(
        AppDbContext dbContext,
        IReadOnlyList<CurriculumSubjectSpec> offerings,
        CancellationToken cancellationToken)
    {
        var subjects = await dbContext.Subjects.ToListAsync(cancellationToken);
        var usedIds = new HashSet<int>();
        var nextId = subjects.Count == 0 ? 1000 : Math.Max(1000, subjects.Max(s => s.Id) + 1);

        ApplyKnownGradeIds(subjects);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.SubjectUnitLessons.ExecuteDeleteAsync(cancellationToken);
        await dbContext.SubjectUnits.ExecuteDeleteAsync(cancellationToken);

        foreach (var spec in offerings
            .OrderBy(x => x.StageId)
            .ThenBy(x => x.Grade)
            .ThenBy(x => x.Term)
            .ThenBy(x => x.TrackCode)
            .ThenBy(x => x.Title))
        {
            var match = subjects.FirstOrDefault(s =>
                    !usedIds.Contains(s.Id)
                    && s.GradeId == spec.Grade
                    && s.TermId == spec.Term
                    && s.Code.Equals(spec.SubjectCode, StringComparison.OrdinalIgnoreCase)
                    && (s.TrackCode ?? "") == spec.TrackCode);

            if (match is null && spec.Term == 1 && spec.TrackCode.Length == 0)
            {
                var aliases = TitleAliasesFor(spec.SubjectCode, spec.Grade);
                foreach (var title in aliases)
                {
                    if (ExternalSubjectIds.TryGetValue((title, spec.Grade), out var preferred)
                        && subjects.FirstOrDefault(s => s.Id == preferred && !usedIds.Contains(s.Id)) is { } preferredRow)
                    {
                        match = preferredRow;
                        break;
                    }
                }

                match ??= subjects.FirstOrDefault(s =>
                    !usedIds.Contains(s.Id)
                    && s.GradeId == spec.Grade
                    && aliases.Contains(s.Title));
            }

            if (match is null)
            {
                match = new Subject
                {
                    Id = nextId++,
                    Title = Clip(spec.Title, 200),
                    StageId = spec.StageId,
                    Units = []
                };
                dbContext.Subjects.Add(match);
                subjects.Add(match);
            }

            usedIds.Add(match.Id);
            match.Title = Clip(spec.Title, 200);
            match.Code = spec.SubjectCode;
            match.Category = Clip(spec.Category, 40);
            match.NameEn = Clip(spec.NameEn, 200);
            match.Notes = Clip(spec.Notes, 1000);
            match.StageId = spec.StageId;
            match.GradeId = spec.Grade;
            match.TermId = spec.Term;
            match.TrackCode = spec.TrackCode;
            match.TrackName = Clip(spec.TrackName, 80);
            match.VerificationStatus = Clip(spec.VerificationStatus, 80);
            match.SourceTocUrl = Clip(spec.SourceTocUrl, 500);
            match.Variants = Clip(spec.Variants, 400);
            SyncSubjectUnits(match, spec);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void SyncSubjectUnits(Subject subject, CurriculumSubjectSpec spec)
    {
        foreach (var unitSpec in spec.Units)
        {
            var unit = new SubjectUnit
            {
                SubjectId = subject.Id,
                Title = Clip(unitSpec.Title, 300),
                SortOrder = Math.Max(1, unitSpec.Index),
                VerificationStatus = Clip(unitSpec.VerificationStatus, 80),
                Lessons = []
            };

            foreach (var lessonSpec in unitSpec.Lessons)
            {
                unit.Lessons.Add(new SubjectUnitLesson
                {
                    Title = Clip(lessonSpec.Title, 300),
                    SortOrder = Math.Max(1, lessonSpec.Index)
                });
            }

            subject.Units.Add(unit);
        }
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
                    TermId = CourseTerm.FullYear,
                    AgeMin = ageMin,
                    AgeMax = ageMax,
                    SortOrder = sortOrder++,
                };
                dbContext.Courses.Add(course);
                courses.Add(course);
            }

            used.Add(course.Id);
            ApplyCourseMetadata(course, spec);
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
        course.TermId = CourseTerm.FullYear;
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
        await FillMissingGradeIdsFromCoursesAsync(dbContext, cancellationToken);
    }

    private static void ApplyKnownGradeIds(IEnumerable<Subject> subjects)
    {
        var byId = subjects.ToDictionary(s => s.Id);
        foreach (var ((_, grade), id) in ExternalSubjectIds)
        {
            if (byId.TryGetValue(id, out var subject))
            {
                subject.GradeId = grade;
            }
        }
    }

    private static async Task FillMissingGradeIdsFromCoursesAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var subjects = await dbContext.Subjects.ToListAsync(cancellationToken);
        ApplyKnownGradeIds(subjects);

        var courseGrades = await dbContext.Courses
            .Where(c => c.ExternalSubjectId != null && c.Grade != null)
            .Select(c => new { c.ExternalSubjectId, c.Grade })
            .ToListAsync(cancellationToken);

        var gradeBySubject = courseGrades
            .GroupBy(c => c.ExternalSubjectId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Grade!.Value).Min());

        foreach (var subject in subjects)
        {
            if (subject.GradeId is not null)
            {
                continue;
            }

            if (gradeBySubject.TryGetValue(subject.Id, out var grade))
            {
                subject.GradeId = grade;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static int? ResolveSubjectId(Course course, IReadOnlyList<Subject> subjects)
    {
        IEnumerable<Subject> pool = subjects;
        if (course.Grade is int grade)
        {
            pool = subjects.Where(s => s.GradeId == grade);
        }
        else if (course.StageId is int stage)
        {
            pool = subjects.Where(s => s.StageId == stage);
        }

        var courseTrack = course.TrackCode ?? "";
        var ranked = pool
            .Select(subject =>
            {
                var titleMatch = TitlesLike(course.Title, subject.Title);
                var codeMatch = !string.IsNullOrWhiteSpace(course.SubjectCode)
                    && subject.Code.Equals(course.SubjectCode, StringComparison.OrdinalIgnoreCase);
                var trackMatch = (subject.TrackCode ?? "") == courseTrack;
                return (subject, titleMatch, codeMatch, trackMatch);
            })
            .Where(x => x.titleMatch || x.codeMatch)
            .OrderBy(x => x.titleMatch ? 0 : 1)
            .ThenBy(x => x.trackMatch ? 0 : 1)
            .ThenBy(x => x.subject.TermId ?? 99)
            .ThenBy(x => x.subject.Id)
            .Select(x => (int?)x.subject.Id)
            .FirstOrDefault();

        return ranked;
    }

    private static bool TitlesLike(string courseTitle, string subjectTitle)
    {
        var courseNames = ExpandTitles(courseTitle);
        var subjectNames = ExpandTitles(subjectTitle);
        if (courseNames.Overlaps(subjectNames))
        {
            return true;
        }

        foreach (var courseName in courseNames)
        {
            foreach (var subjectName in subjectNames)
            {
                if (courseName.Length < 3 || subjectName.Length < 3)
                {
                    continue;
                }

                if (courseName.Contains(subjectName, StringComparison.OrdinalIgnoreCase)
                    || subjectName.Contains(courseName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static HashSet<string> ExpandTitles(string title)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddTitle(names, title);
        foreach (var name in names.ToList())
        {
            if (TitleAliases.TryGetValue(name, out var mapped))
            {
                AddTitle(names, mapped);
            }

            foreach (var pair in TitleAliases)
            {
                if (pair.Value.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    AddTitle(names, pair.Key);
                }
            }
        }

        foreach (var code in TitleAliasCodes)
        {
            var aliases = TitleAliasesFor(code, 5);
            aliases.UnionWith(TitleAliasesFor(code, 10));
            if (names.Overlaps(aliases))
            {
                names.UnionWith(aliases);
            }
        }

        return names;
    }

    private static void AddTitle(HashSet<string> names, string? title)
    {
        var value = title?.Trim();
        if (!string.IsNullOrWhiteSpace(value))
        {
            names.Add(value);
        }
    }

    private static readonly string[] TitleAliasCodes =
    [
        "arabic", "english", "first_foreign_language", "math", "discover", "science",
        "social_studies", "islamic_religion", "religion", "history", "geography",
        "physics", "chemistry", "biology"
    ];

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
