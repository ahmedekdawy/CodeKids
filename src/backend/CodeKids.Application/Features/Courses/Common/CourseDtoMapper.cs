using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;

namespace CodeKids.Application.Features.Courses;

internal static class CourseDtoMapper
{
    public static CourseDto Map(Course course, bool includeContent = true, CourseContentOutline? outline = null)
    {
        if (!includeContent)
        {
            return Create(
                course,
                Array.Empty<CourseUnitDto>(),
                Array.Empty<CourseLessonDto>(),
                Array.Empty<CourseQuizDto>());
        }

        var content = outline ?? new CourseContentOutline(
            course.Units
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Title)
                .Select(unit => new CourseUnitDto(
                    unit.Id,
                    unit.CourseId,
                    unit.Title,
                    unit.Description,
                    unit.SortOrder,
                    course.Lessons
                        .Where(l => l.UnitId == unit.Id)
                        .OrderBy(l => l.SortOrder)
                        .Select(MapLesson)
                        .ToList(),
                    (int?)unit.TermId,
                    unit.VerificationStatus,
                    unit.StudentAskEnabled))
                .ToList(),
            course.Lessons.OrderBy(x => x.SortOrder).Select(MapLesson).ToList());

        var quizzes = course.Quizzes
            .Select(quiz => new CourseQuizDto(
                quiz.Id,
                quiz.Title,
                quiz.Description,
                quiz.XpReward,
                quiz.Questions.Count))
            .ToList();

        return Create(course, content.Units, content.Lessons, quizzes);
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
            course.TermId?.ToString(),
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
            course.Variants,
            course.StudentAskEnabled);

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
}
