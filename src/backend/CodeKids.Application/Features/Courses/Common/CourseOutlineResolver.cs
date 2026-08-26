using System.Security.Cryptography;
using System.Text;
using CodeKids.Application.Abstractions;
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
                        lesson.Title,
                        course.Theme,
                        $"{lesson.Title} — {subject.Title}",
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
                    unit.Title,
                    subject.TermId is int term ? $"{unit.Title} — الترم {term}" : unit.Title,
                    unit.SortOrder,
                    unitLessons,
                    subject.TermId,
                    unit.VerificationStatus));
            }
        }

        return new CourseContentOutline(units, lessons);
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
                Title = unit.Title,
                Description = unit.Description,
                SortOrder = unit.SortOrder,
                TermId = unit.Term is int term ? (Domain.Enums.CourseTerm)term : null,
                VerificationStatus = unit.VerificationStatus,
                Lessons = []
            };
            foreach (var lesson in unit.Lessons)
            {
                var lessonEntity = new Lesson
                {
                    Id = lesson.Id,
                    CourseId = course.Id,
                    UnitId = unit.Id,
                    Title = lesson.Title,
                    Theme = lesson.Theme,
                    Description = lesson.Description,
                    Difficulty = lesson.Difficulty,
                    XpReward = lesson.XpReward,
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
