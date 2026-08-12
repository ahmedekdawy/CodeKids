using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
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
