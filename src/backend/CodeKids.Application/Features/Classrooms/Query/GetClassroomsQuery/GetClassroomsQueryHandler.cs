using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Admin;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

public sealed class GetClassroomsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetClassroomsQuery, IReadOnlyList<ClassroomDto>>
{
    public async Task<IReadOnlyList<ClassroomDto>> Handle(GetClassroomsQuery query, CancellationToken cancellationToken)
    {
        var classrooms = await dbContext.Classrooms
            .AsNoTracking()
            .Include(x => x.Courses)
                .ThenInclude(x => x.Course)
            .Include(x => x.Courses)
                .ThenInclude(x => x.Teacher)
            .Include(x => x.Course)
            .Include(x => x.Students)
                .ThenInclude(x => x.Student)
            .Include(x => x.CourseEnrollments)
                .ThenInclude(x => x.Course)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        if (string.Equals(query.ViewerRole, nameof(UserRole.Teacher), StringComparison.OrdinalIgnoreCase))
        {
            classrooms = classrooms
                .Where(x => CreateClassroomCommandHandler.HasTeacher(x, query.ViewerUserId))
                .ToList();
        }
        else if (string.Equals(query.ViewerRole, nameof(UserRole.Student), StringComparison.OrdinalIgnoreCase))
        {
            classrooms = classrooms
                .Where(x => x.Students.Any(s => s.StudentId == query.ViewerUserId))
                .ToList();

            foreach (var classroom in classrooms)
            {
                var enrolled = StudentCourseVisibility.EnrolledCourseIdsForClassroom(
                    classroom.CourseEnrollments, query.ViewerUserId, classroom.Id);
                if (enrolled.Count == 0)
                {
                    continue;
                }

                classroom.Courses = classroom.Courses.Where(c => enrolled.Contains(c.CourseId)).ToList();
            }
        }
        else if (string.Equals(query.ViewerRole, nameof(UserRole.Parent), StringComparison.OrdinalIgnoreCase))
        {
            var childIds = await dbContext.Users
                .Where(x => x.ParentId == query.ViewerUserId && x.Role == UserRole.Student)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            classrooms = classrooms
                .Where(x => x.Students.Any(s => childIds.Contains(s.StudentId)))
                .ToList();
        }

        return classrooms.Select(CreateClassroomCommandHandler.Map).ToList();
    }
}
