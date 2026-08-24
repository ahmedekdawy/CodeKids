using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;

namespace CodeKids.Application.Features.Courses;

internal static class CourseDtoMapper
{
    public static CourseDto Map(Course course, bool includeContent = true)
    {
        if (!includeContent)
        {
            return Create(
                course,
                Array.Empty<CourseUnitDto>(),
                Array.Empty<CourseLessonDto>(),
                Array.Empty<CourseQuizDto>());
        }

        var lessons = course.Lessons
            .OrderBy(x => x.SortOrder)
            .Select(MapLesson)
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
                lessons.Where(l => l.UnitId == unit.Id).ToList(),
                unit.Term,
                unit.VerificationStatus))
            .ToList();

        var quizzes = course.Quizzes
            .Select(quiz => new CourseQuizDto(
                quiz.Id,
                quiz.Title,
                quiz.Description,
                quiz.XpReward,
                quiz.Questions.Count))
            .ToList();

        return Create(course, units, lessons, quizzes);
    }

    private static CourseDto Create(
        Course course,
        IReadOnlyList<CourseUnitDto> units,
        IReadOnlyList<CourseLessonDto> lessons,
        IReadOnlyList<CourseQuizDto> quizzes) =>
        new(
            course.Id,
            course.Title,
            course.Theme,
            course.Description,
            course.AgeMin,
            course.AgeMax,
            course.Term?.ToString(),
            course.Grade,
            course.StageId,
            course.SchoolType?.ToString() ?? nameof(SchoolType.All),
            course.SortOrder,
            course.ExternalSubjectId,
            units,
            lessons,
            quizzes,
            course.SubjectCode,
            course.Category,
            course.TrackCode,
            course.TrackName,
            course.VerificationStatus,
            course.SourceTocUrl,
            course.Notes,
            course.Variants);

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
            lesson.Steps.Count);
}
