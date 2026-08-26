using System.Security.Cryptography;
using System.Text;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Lessons;
using CodeKids.Application.Features.StudentAsk;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Courses;

internal sealed record CourseContentOutline(
    IReadOnlyList<CourseUnitDto> Units,
    IReadOnlyList<CourseLessonDto> Lessons);

internal static class CourseOutlineResolver
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
        var needingFallback = new List<Course>();

        foreach (var course in courses)
        {
            if (course.Units.Count > 0 || course.Lessons.Count > 0)
            {
                result[course.Id] = FromCourse(course);
            }
            else
            {
                needingFallback.Add(course);
            }
        }

        if (needingFallback.Count == 0)
        {
            return result;
        }

        var subjectIds = needingFallback
            .Where(c => c.ExternalSubjectId is not null)
            .Select(c => c.ExternalSubjectId!.Value)
            .Distinct()
            .ToList();
        var grades = needingFallback.Where(c => c.Grade is not null).Select(c => c.Grade!.Value).Distinct().ToList();
        var codes = needingFallback
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

        foreach (var course in needingFallback)
        {
            var related = subjects
                .Where(s => IsRelatedSubject(course, s))
                .OrderBy(s => s.TermId ?? 99)
                .ThenBy(s => s.Id)
                .ToList();
            result[course.Id] = related.Count == 0
                ? FromCourse(course)
                : FromSubjects(course, related);
        }

        return result;
    }

    private static bool IsRelatedSubject(Course course, Subject subject)
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

    private static CourseContentOutline FromCourse(Course course)
    {
        var lessons = course.Lessons
            .OrderBy(x => x.SortOrder)
            .Select(MapLesson)
            .ToList();

        var unitLessons = course.Units
            .SelectMany(unit => unit.Lessons ?? [])
            .Select(MapLesson)
            .ToList();

        var allLessons = lessons
            .Concat(unitLessons)
            .GroupBy(l => l.Id)
            .Select(g => g.First())
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.Title)
            .ToList();

        var units = course.Units
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title)
            .Select(unit => new CourseUnitDto(
                unit.Id,
                unit.CourseId,
                unit.Title,
                unit.Description,
                unit.SortOrder,
                allLessons.Where(l => l.UnitId == unit.Id).ToList(),
                (int?)unit.TermId,
                unit.VerificationStatus,
                unit.StudentAskEnabled))
            .ToList();

        return new CourseContentOutline(units, allLessons);
    }

    private static CourseContentOutline FromSubjects(Course course, IReadOnlyList<Subject> subjects)
    {
        var units = new List<CourseUnitDto>();
        var lessons = new List<CourseLessonDto>();

        foreach (var subject in subjects)
        {
            foreach (var unit in subject.Units.OrderBy(u => u.SortOrder).ThenBy(u => u.Title))
            {
                var unitId = StableId("unit", course.Id, subject.TermId, unit.SortOrder, unit.Title);
                var unitLessons = new List<CourseLessonDto>();
                foreach (var lesson in unit.Lessons.OrderBy(l => l.SortOrder).ThenBy(l => l.Title))
                {
                    var mapped = new CourseLessonDto(
                        StableId("lesson", course.Id, subject.TermId, unit.SortOrder, lesson.SortOrder, lesson.Title),
                        unitId,
                        Clamp(lesson.Title, 300),
                        Clamp(course.Theme, 60),
                        Clamp($"{lesson.Title} — {subject.Title}", 500),
                        Math.Clamp(1 + (course.Grade ?? 1) / 3, 1, 5),
                        20 + (course.Grade ?? 1) * 2,
                        lesson.SortOrder,
                        0,
                        false);
                    unitLessons.Add(mapped);
                    lessons.Add(mapped);
                }

                units.Add(new CourseUnitDto(
                    unitId,
                    course.Id,
                    Clamp(unit.Title, 300),
                    Clamp(
                        subject.TermId is int term ? $"{unit.Title} — الترم {term}" : unit.Title,
                        500),
                    unit.SortOrder,
                    unitLessons,
                    subject.TermId,
                    Clamp(unit.VerificationStatus, 80)));
            }
        }

        return new CourseContentOutline(units, lessons);
    }

    public static async Task<Lesson?> LoadStoredLessonAsync(
        IAppDbContext dbContext,
        Guid lessonId,
        CancellationToken cancellationToken) =>
        await dbContext.Lessons
            .Include(x => x.Steps)
            .Include(x => x.Videos)
            .Include(x => x.Unit)
            .Include(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == lessonId, cancellationToken);

    public static async Task<LessonDto?> ResolvePlayableLessonAsync(
        IAppDbContext dbContext,
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var stored = await LoadStoredLessonAsync(dbContext, lessonId, cancellationToken);
        if (stored is not null)
        {
            return MapPlayable(stored);
        }

        var fallback = await FindFallbackLessonAsync(dbContext, lessonId, cancellationToken);
        if (fallback is null)
        {
            return null;
        }

        try
        {
            await AttachFallbackUnitsAsync(dbContext, fallback.Value.Course, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            stored = await LoadStoredLessonAsync(dbContext, lessonId, cancellationToken);
            if (stored is not null)
            {
                return MapPlayable(stored);
            }
        }
        catch
        {
            // Catalog rows can fail validation; still return a playable DTO.
        }

        var course = fallback.Value.Course;
        var lesson = fallback.Value.Lesson;
        return new LessonDto(
            lesson.Id,
            course.Id,
            lesson.Title,
            lesson.Theme,
            lesson.Description,
            lesson.Difficulty,
            lesson.XpReward,
            [],
            [],
            lesson.UnitId,
            StudentAskAccess.IsEnabled(course, null, null));
    }

    private static async Task<(Course Course, CourseUnitDto Unit, CourseLessonDto Lesson)?> FindFallbackLessonAsync(
        IAppDbContext dbContext,
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var candidates = await dbContext.Courses
            .Include(x => x.Units)
            .Include(x => x.Lessons)
            .Where(x => !x.Units.Any() && !x.Lessons.Any())
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return null;
        }

        var outlines = await ResolveManyAsync(dbContext, candidates, cancellationToken);
        foreach (var course in candidates)
        {
            var outline = outlines[course.Id];
            var lesson = outline.Lessons.FirstOrDefault(l => l.Id == lessonId);
            if (lesson is null)
            {
                continue;
            }

            var unit = outline.Units.FirstOrDefault(u => u.Id == lesson.UnitId || u.Lessons.Any(l => l.Id == lessonId));
            if (unit is null)
            {
                continue;
            }

            return (course, unit, lesson);
        }

        return null;
    }

    internal static LessonDto MapPlayable(Lesson lesson) =>
        new(
            lesson.Id,
            lesson.CourseId,
            lesson.Title,
            lesson.Theme,
            lesson.Description,
            lesson.Difficulty,
            lesson.XpReward,
            lesson.Steps
                .OrderBy(step => step.StepNumber)
                .Select(step => new LessonStepDto(step.Id, step.StepNumber, step.Title, step.Prompt))
                .ToList(),
            (lesson.Videos ?? [])
                .OrderBy(v => v.SortOrder)
                .ThenBy(v => v.CreatedAtUtc)
                .Select(v => new LessonVideoSummaryDto(
                    v.Id,
                    v.MediaAssetId,
                    v.Title,
                    v.SortOrder,
                    null))
                .ToList(),
            lesson.UnitId,
            StudentAskAccess.IsEnabled(lesson.Course, lesson.Unit, lesson));

    private static string Clamp(string? value, int max)
    {
        var text = (value ?? string.Empty).Trim();
        return text.Length <= max ? text : text[..max];
    }

    public static async Task AttachFallbackUnitsAsync(
        IAppDbContext dbContext,
        Course course,
        CancellationToken cancellationToken)
    {
        if (course.Units.Count > 0 || course.Lessons.Count > 0)
        {
            return;
        }

        var outline = await ResolveAsync(dbContext, course, cancellationToken);
        foreach (var unit in outline.Units)
        {
            var entity = new CourseUnit
            {
                Id = unit.Id,
                CourseId = course.Id,
                Title = Clamp(unit.Title, 300),
                Description = Clamp(unit.Description, 500),
                SortOrder = unit.SortOrder,
                TermId = unit.Term is int term && Enum.IsDefined(typeof(Domain.Enums.CourseTerm), term)
                    ? (Domain.Enums.CourseTerm)term
                    : null,
                VerificationStatus = Clamp(unit.VerificationStatus, 80),
                Lessons = []
            };
            foreach (var lesson in unit.Lessons)
            {
                var lessonEntity = new Lesson
                {
                    Id = lesson.Id,
                    CourseId = course.Id,
                    UnitId = unit.Id,
                    Title = Clamp(lesson.Title, 300),
                    Theme = Clamp(string.IsNullOrWhiteSpace(lesson.Theme) ? "General" : lesson.Theme, 60),
                    Description = Clamp(lesson.Description, 500),
                    Difficulty = Math.Clamp(lesson.Difficulty, 1, 5),
                    XpReward = Math.Max(0, lesson.XpReward),
                    SortOrder = lesson.SortOrder
                };
                entity.Lessons.Add(lessonEntity);
                course.Lessons.Add(lessonEntity);
            }

            course.Units.Add(entity);
        }
    }

    private static CourseLessonDto MapLesson(Lesson lesson) =>
        new(
            lesson.Id,
            lesson.UnitId,
            lesson.Title,
            lesson.Theme,
            lesson.Description,
            lesson.Difficulty,
            lesson.XpReward,
            lesson.SortOrder,
            lesson.Steps.Count,
            lesson.StudentAskEnabled);

    private static Guid StableId(params object?[] parts)
    {
        var key = "course-subject-fallback:" + string.Join("|", parts.Select(p => p?.ToString() ?? ""));
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(key));
        hash[6] = (byte)((hash[6] & 0x0f) | 0x40);
        hash[8] = (byte)((hash[8] & 0x3f) | 0x80);
        return new Guid(hash);
    }
}
