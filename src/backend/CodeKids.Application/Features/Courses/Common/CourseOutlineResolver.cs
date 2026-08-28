using System.Security.Cryptography;
using System.Text;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Lessons;
using CodeKids.Application.Features.StudentAsk;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Courses;

public sealed record CourseContentOutline(
    IReadOnlyList<CourseUnitDto> Units,
    IReadOnlyList<CourseLessonDto> Lessons);

public sealed record CatalogUnitRef(Course Course, Subject Subject, SubjectUnit Unit);

public sealed record CatalogLessonRef(Course Course, Subject Subject, SubjectUnit Unit, SubjectUnitLesson Lesson);

public static class CourseOutlineResolver
{
    public static async Task<CourseContentOutline> ResolveAsync(
        IAppDbContext dbContext,
        Course course,
        CancellationToken cancellationToken)
    {
        var map = await ResolveManyAsync(dbContext, [course], cancellationToken);
        return map[course.Id];
    }

    public static async Task<IReadOnlyDictionary<Guid, CourseContentOutline>> ResolveManyAsync(
        IAppDbContext dbContext,
        IReadOnlyList<Course> courses,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, CourseContentOutline>();
        if (courses.Count == 0)
        {
            return result;
        }

        var subjectIds = courses
            .Where(c => c.ExternalSubjectId is not null)
            .Select(c => c.ExternalSubjectId!.Value)
            .Distinct()
            .ToList();
        var grades = courses.Where(c => c.Grade is not null).Select(c => c.Grade!.Value).Distinct().ToList();
        var codes = courses
            .Where(c => !string.IsNullOrWhiteSpace(c.SubjectCode))
            .Select(c => c.SubjectCode)
            .Distinct()
            .ToList();

        var subjects = await dbContext.Subjects
            .AsNoTracking()
            .Include(s => s.Units)
                .ThenInclude(u => u.Lessons)
            .Where(s => subjectIds.Contains(s.Id)
                || (s.GradeId != null && grades.Contains(s.GradeId.Value) && codes.Contains(s.Code)))
            .ToListAsync(cancellationToken);

        foreach (var course in courses)
        {
            var related = subjects
                .Where(s => IsRelatedSubject(course, s))
                .OrderBy(s => s.TermId ?? 99)
                .ThenBy(s => s.Id)
                .ToList();
            result[course.Id] = FromSubjects(course, related);
        }

        return result;
    }

    public static async Task<IReadOnlyDictionary<Guid, (Course Course, CourseLessonDto Lesson, bool UnitAskEnabled)>> IndexLessonsAsync(
        IAppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var courses = await dbContext.Courses.AsNoTracking().ToListAsync(cancellationToken);
        var outlines = await ResolveManyAsync(dbContext, courses, cancellationToken);
        var map = new Dictionary<Guid, (Course Course, CourseLessonDto Lesson, bool UnitAskEnabled)>();
        foreach (var course in courses)
        {
            var outline = outlines[course.Id];
            foreach (var unit in outline.Units)
            {
                foreach (var lesson in unit.Lessons)
                {
                    map[lesson.Id] = (course, lesson, unit.StudentAskEnabled);
                }
            }
        }

        return map;
    }

    public static bool IsRelatedSubject(Course course, Subject subject)
    {
        if (course.ExternalSubjectId is int subjectId && subject.Id == subjectId)
        {
            return true;
        }

        if (course.Grade is int grade
            && !string.IsNullOrWhiteSpace(course.SubjectCode)
            && subject.GradeId == grade
            && subject.Code.Equals(course.SubjectCode, StringComparison.OrdinalIgnoreCase)
            && (subject.TrackCode ?? "") == (course.TrackCode ?? ""))
        {
            return true;
        }

        return false;
    }

    public static CourseContentOutline FromSubjects(Course course, IReadOnlyList<Subject> subjects)
    {
        var units = new List<CourseUnitDto>();
        var lessons = new List<CourseLessonDto>();

        foreach (var subject in subjects)
        {
            foreach (var unit in subject.Units.OrderBy(u => u.SortOrder).ThenBy(u => u.Title))
            {
                var mappedUnit = MapUnit(course, subject, unit);
                units.Add(mappedUnit);
                lessons.AddRange(mappedUnit.Lessons);
            }
        }

        return new CourseContentOutline(units, lessons);
    }

    public static CourseUnitDto MapUnit(Course course, Subject subject, SubjectUnit unit)
    {
        var unitId = UnitId(course, subject, unit);
        var unitLessons = unit.Lessons
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.Title)
            .Select(lesson => MapLesson(course, subject, unit, lesson, unitId))
            .ToList();

        return new CourseUnitDto(
            unitId,
            course.Id,
            Clamp(unit.Title, 300),
            Clamp(
                subject.TermId is int term ? $"{unit.Title} — الترم {term}" : unit.Title,
                500),
            unit.SortOrder,
            unitLessons,
            subject.TermId,
            Clamp(unit.VerificationStatus, 80),
            unit.StudentAskEnabled);
    }

    public static CourseLessonDto MapLesson(
        Course course,
        Subject subject,
        SubjectUnit unit,
        SubjectUnitLesson lesson,
        Guid? unitId = null)
    {
        var resolvedUnitId = unitId ?? UnitId(course, subject, unit);
        return new CourseLessonDto(
            LessonId(course, subject, unit, lesson),
            resolvedUnitId,
            Clamp(lesson.Title, 300),
            Clamp(string.IsNullOrWhiteSpace(course.Theme) ? "General" : course.Theme, 60),
            Clamp($"{lesson.Title} — {subject.Title}", 500),
            Math.Clamp(1 + (course.Grade ?? 1) / 3, 1, 5),
            20 + (course.Grade ?? 1) * 2,
            lesson.SortOrder,
            0,
            lesson.StudentAskEnabled);
    }

    public static Guid UnitId(Course course, Subject subject, SubjectUnit unit) =>
        StableId("unit", course.Id, subject.TermId, unit.SortOrder, unit.Title);

    public static Guid LessonId(Course course, Subject subject, SubjectUnit unit, SubjectUnitLesson lesson) =>
        StableId("lesson", course.Id, subject.TermId, unit.SortOrder, lesson.SortOrder, lesson.Title);

    public static async Task<CatalogUnitRef?> FindUnitAsync(
        IAppDbContext dbContext,
        Guid unitId,
        CancellationToken cancellationToken)
    {
        var courses = await dbContext.Courses.ToListAsync(cancellationToken);
        var outlines = await ResolveManyAsync(dbContext, courses, cancellationToken);
        foreach (var course in courses)
        {
            if (!outlines[course.Id].Units.Any(u => u.Id == unitId))
            {
                continue;
            }

            var subjects = await LoadRelatedSubjectsAsync(dbContext, course, cancellationToken);
            foreach (var subject in subjects)
            {
                foreach (var unit in subject.Units)
                {
                    if (UnitId(course, subject, unit) == unitId)
                    {
                        return new CatalogUnitRef(course, subject, unit);
                    }
                }
            }
        }

        return null;
    }

    public static async Task<CatalogLessonRef?> FindLessonAsync(
        IAppDbContext dbContext,
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var courses = await dbContext.Courses.ToListAsync(cancellationToken);
        var outlines = await ResolveManyAsync(dbContext, courses, cancellationToken);
        foreach (var course in courses)
        {
            if (!outlines[course.Id].Lessons.Any(l => l.Id == lessonId))
            {
                continue;
            }

            var subjects = await LoadRelatedSubjectsAsync(dbContext, course, cancellationToken);
            foreach (var subject in subjects)
            {
                foreach (var unit in subject.Units)
                {
                    foreach (var lesson in unit.Lessons)
                    {
                        if (LessonId(course, subject, unit, lesson) == lessonId)
                        {
                            return new CatalogLessonRef(course, subject, unit, lesson);
                        }
                    }
                }
            }
        }

        return null;
    }

    public static async Task<IReadOnlyList<Subject>> LoadRelatedSubjectsAsync(
        IAppDbContext dbContext,
        Course course,
        CancellationToken cancellationToken)
    {
        return await dbContext.Subjects
            .Include(s => s.Units)
                .ThenInclude(u => u.Lessons)
            .Where(s =>
                (course.ExternalSubjectId != null && s.Id == course.ExternalSubjectId)
                || (course.Grade != null
                    && s.GradeId == course.Grade
                    && s.Code == course.SubjectCode
                    && (s.TrackCode ?? "") == (course.TrackCode ?? "")))
            .OrderBy(s => s.TermId ?? 99)
            .ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);
    }

    public static async Task<LessonDto?> ResolvePlayableLessonAsync(
        IAppDbContext dbContext,
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var found = await FindLessonAsync(dbContext, lessonId, cancellationToken);
        if (found is null)
        {
            return null;
        }

        var mapped = MapLesson(found.Course, found.Subject, found.Unit, found.Lesson);
        return await ToPlayableAsync(dbContext, found.Course, mapped, found.Unit.StudentAskEnabled, cancellationToken);
    }

    public static async Task<LessonDto> ToPlayableAsync(
        IAppDbContext dbContext,
        Course course,
        CourseLessonDto lesson,
        bool unitAskEnabled,
        CancellationToken cancellationToken)
    {
        var steps = await dbContext.LessonSteps
            .AsNoTracking()
            .Where(x => x.LessonId == lesson.Id)
            .OrderBy(x => x.StepNumber)
            .ToListAsync(cancellationToken);
        var videos = await dbContext.LessonVideos
            .AsNoTracking()
            .Where(x => x.LessonId == lesson.Id)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return new LessonDto(
            lesson.Id,
            course.Id,
            lesson.Title,
            lesson.Theme,
            lesson.Description,
            lesson.Difficulty,
            lesson.XpReward,
            steps.Select(step => new LessonStepDto(step.Id, step.StepNumber, step.Title, step.Prompt)).ToList(),
            videos.Select(v => new LessonVideoSummaryDto(v.Id, v.MediaAssetId, v.Title, v.SortOrder, null)).ToList(),
            lesson.UnitId,
            StudentAskAccess.IsEnabled(course, unitAskEnabled, lesson.StudentAskEnabled));
    }

    public static string Clamp(string? value, int max)
    {
        var text = (value ?? string.Empty).Trim();
        return text.Length <= max ? text : text[..max];
    }

    private static Guid StableId(params object?[] parts)
    {
        var key = "course-subject-fallback:" + string.Join("|", parts.Select(p => p?.ToString() ?? ""));
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(key));
        hash[6] = (byte)((hash[6] & 0x0f) | 0x40);
        hash[8] = (byte)((hash[8] & 0x3f) | 0x80);
        return new Guid(hash);
    }
}
