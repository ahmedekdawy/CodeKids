using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Courses;

public sealed record CourseLessonDto(
    Guid Id,
    string Title,
    string Theme,
    string Description,
    int Difficulty,
    int XpReward,
    int SortOrder,
    int StepCount);

public sealed record CourseQuizDto(Guid Id, string Title, string Description, int XpReward, int QuestionCount);

public sealed record CourseDto(
    Guid Id,
    string Title,
    string Theme,
    string Description,
    int AgeMin,
    int AgeMax,
    string Term,
    int Grade,
    int SortOrder,
    IReadOnlyList<CourseLessonDto> Lessons,
    IReadOnlyList<CourseQuizDto> Quizzes);

public sealed record GetCoursesQuery() : IQuery<IReadOnlyList<CourseDto>>;

public sealed class GetCoursesQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetCoursesQuery, IReadOnlyList<CourseDto>>
{
    public async Task<IReadOnlyList<CourseDto>> Handle(GetCoursesQuery query, CancellationToken cancellationToken)
    {
        var courses = await dbContext.Courses
            .AsNoTracking()
            .Include(x => x.Lessons)
                .ThenInclude(x => x.Steps)
            .Include(x => x.Quizzes)
                .ThenInclude(x => x.Questions)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        return courses.Select(course => new CourseDto(
            course.Id,
            course.Title,
            course.Theme,
            course.Description,
            course.AgeMin,
            course.AgeMax,
            course.Term.ToString(),
            course.Grade,
            course.SortOrder,
            course.Lessons
                .OrderBy(x => x.SortOrder)
                .Select(lesson => new CourseLessonDto(
                    lesson.Id,
                    lesson.Title,
                    lesson.Theme,
                    lesson.Description,
                    lesson.Difficulty,
                    lesson.XpReward,
                    lesson.SortOrder,
                    lesson.Steps.Count))
                .ToList(),
            course.Quizzes
                .Select(quiz => new CourseQuizDto(
                    quiz.Id,
                    quiz.Title,
                    quiz.Description,
                    quiz.XpReward,
                    quiz.Questions.Count))
                .ToList())).ToList();
    }
}

