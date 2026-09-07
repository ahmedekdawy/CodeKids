using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;

namespace CodeKids.Application.Features.Courses;

internal static class CourseDtoMapper
{
    public static CourseDto Map(
        Course course,
        bool includeContent = true,
        CourseContentOutline? outline = null,
        IReadOnlyList<CourseVideoSummaryDto>? videos = null,
        bool includeUnpublishedQuizzes = true)
    {
        if (!includeContent)
        {
            return Create(
                course,
                Array.Empty<CourseUnitDto>(),
                Array.Empty<CourseLessonDto>(),
                Array.Empty<CourseQuizDto>(),
                Array.Empty<CourseVideoSummaryDto>());
        }

        var content = outline ?? new CourseContentOutline([], []);
        var quizzes = course.Quizzes
            .Where(quiz => includeUnpublishedQuizzes || quiz.IsPublished)
            .Select(quiz => new CourseQuizDto(
                quiz.Id,
                quiz.Title,
                quiz.Description,
                quiz.XpReward,
                quiz.Questions.Count,
                quiz.IsPublished))
            .ToList();

        return Create(course, content.Units, content.Lessons, quizzes, videos ?? []);
    }

    private static CourseDto Create(
        Course course,
        IReadOnlyList<CourseUnitDto> units,
        IReadOnlyList<CourseLessonDto> lessons,
        IReadOnlyList<CourseQuizDto> quizzes,
        IReadOnlyList<CourseVideoSummaryDto> videos) =>
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
            videos,
            course.SubjectCode,
            course.Category,
            course.TrackCode,
            course.TrackName,
            course.VerificationStatus,
            course.SourceTocUrl,
            course.Notes,
            course.Variants,
            course.StudentAskEnabled,
            course.IsPublished);
}
