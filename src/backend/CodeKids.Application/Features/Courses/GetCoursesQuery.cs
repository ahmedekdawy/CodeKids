using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using CodeKids.Domain.Enums;
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
    string? Term,
    int? Grade,
    int SortOrder,
    IReadOnlyList<CourseLessonDto> Lessons,
    IReadOnlyList<CourseQuizDto> Quizzes);

public sealed record GetCoursesQuery(Guid? UserId = null, string? Role = null) : IQuery<IReadOnlyList<CourseDto>>;

public sealed class GetCoursesQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetCoursesQuery, IReadOnlyList<CourseDto>>
{
    public async Task<IReadOnlyList<CourseDto>> Handle(GetCoursesQuery query, CancellationToken cancellationToken)
    {
        var coursesQuery = dbContext.Courses
            .AsNoTracking()
            .Include(x => x.Lessons)
                .ThenInclude(x => x.Steps)
            .Include(x => x.Quizzes)
                .ThenInclude(x => x.Questions)
            .AsQueryable();

        // Students see classroom courses that match their grade (or all-grades courses).
        if (query.Role == nameof(UserRole.Student) && query.UserId is Guid studentId)
        {
            var studentGrade = await dbContext.Users
                .AsNoTracking()
                .Where(x => x.Id == studentId)
                .Select(x => x.Grade)
                .FirstOrDefaultAsync(cancellationToken);

            var classroomCourseIds = await dbContext.ClassroomStudents
                .AsNoTracking()
                .Where(x => x.StudentId == studentId)
                .Join(
                    dbContext.ClassroomCourses.AsNoTracking(),
                    cs => cs.ClassroomId,
                    cc => cc.ClassroomId,
                    (_, cc) => cc.CourseId)
                .Distinct()
                .ToListAsync(cancellationToken);

            // Also include legacy classroom.CourseId links until fully migrated.
            var legacyCourseIds = await dbContext.ClassroomStudents
                .AsNoTracking()
                .Where(x => x.StudentId == studentId)
                .Join(
                    dbContext.Classrooms.AsNoTracking(),
                    cs => cs.ClassroomId,
                    c => c.Id,
                    (_, c) => c.CourseId)
                .Where(id => id != null)
                .Select(id => id!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            classroomCourseIds = classroomCourseIds.Concat(legacyCourseIds).Distinct().ToList();

            coursesQuery = coursesQuery.Where(c =>
                classroomCourseIds.Contains(c.Id)
                && (c.Grade == null || studentGrade == null || c.Grade == studentGrade));
        }
        // Teachers see only courses assigned to them on classrooms.
        else if (query.Role == nameof(UserRole.Teacher) && query.UserId is Guid teacherId)
        {
            var teacherCourseIds = await dbContext.ClassroomCourses
                .AsNoTracking()
                .Where(x => x.TeacherId == teacherId)
                .Select(x => x.CourseId)
                .Distinct()
                .ToListAsync(cancellationToken);

            coursesQuery = coursesQuery.Where(c => teacherCourseIds.Contains(c.Id));
        }

        var courses = await coursesQuery
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        return courses.Select(course => new CourseDto(
            course.Id,
            course.Title,
            course.Theme,
            course.Description,
            course.AgeMin,
            course.AgeMax,
            course.Term?.ToString(),
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
