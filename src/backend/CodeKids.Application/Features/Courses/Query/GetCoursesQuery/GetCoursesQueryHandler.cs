using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Classrooms;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Courses;

public sealed class GetCoursesQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetCoursesQuery, IReadOnlyList<CourseDto>>
{
    public async Task<IReadOnlyList<CourseDto>> Handle(GetCoursesQuery query, CancellationToken cancellationToken)
    {
        var coursesQuery = dbContext.Courses
            .AsNoTracking()
            .Include(x => x.Units)
            .Include(x => x.Lessons)
                .ThenInclude(x => x.Steps)
            .Include(x => x.Quizzes)
                .ThenInclude(x => x.Questions)
            .AsQueryable();

        // Students see classroom courses that match their grade (or all-grades courses).
        if (query.Role == nameof(UserRole.Student) && query.UserId is Guid studentId)
        {
            var student = await dbContext.Users
                .AsNoTracking()
                .Where(x => x.Id == studentId)
                .Select(x => new { x.Grade, x.SchoolType })
                .FirstOrDefaultAsync(cancellationToken);

            var visibleCourseIds = await StudentCourseVisibility.GetVisibleCourseIdsAsync(
                dbContext, studentId, cancellationToken);
            var studentGrade = student?.Grade;
            var studentSchoolType = student?.SchoolType;

            coursesQuery = coursesQuery.Where(c =>
                visibleCourseIds.Contains(c.Id)
                && (c.Grade == null || studentGrade == null || c.Grade == studentGrade)
                && (c.SchoolType == null
                    || c.SchoolType == SchoolType.All
                    || studentSchoolType == null
                    || studentSchoolType == SchoolType.All
                    || c.SchoolType == studentSchoolType));
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

        return courses.Select(course =>
        {
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
                    lessons.Where(l => l.UnitId == unit.Id).ToList()))
                .ToList();

            return new CourseDto(
                course.Id,
                course.Title,
                course.Theme,
                course.Description,
                course.AgeMin,
                course.AgeMax,
                course.Term?.ToString(),
                course.Grade,
                course.SchoolType?.ToString() ?? nameof(SchoolType.All),
                course.SortOrder,
                units,
                lessons,
                course.Quizzes
                    .Select(quiz => new CourseQuizDto(
                        quiz.Id,
                        quiz.Title,
                        quiz.Description,
                        quiz.XpReward,
                        quiz.Questions.Count))
                    .ToList());
        }).ToList();
    }

    private static CourseLessonDto MapLesson(Domain.Entities.Lesson lesson) =>
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
