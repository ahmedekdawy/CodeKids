using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Assignments;

public sealed class GetAssignmentsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetAssignmentsQuery, IReadOnlyList<AssignmentDto>>
{
    public async Task<IReadOnlyList<AssignmentDto>> Handle(GetAssignmentsQuery query, CancellationToken cancellationToken)
    {
        var assignments = await dbContext.Assignments
            .AsNoTracking()
            .Include(x => x.Classroom!)
                .ThenInclude(c => c.Courses)
            .Include(x => x.Classroom!)
                .ThenInclude(c => c.Students)
            .Include(x => x.CreatedBy)
            .Include(x => x.Questions)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (query.ClassroomId is Guid classroomId)
        {
            assignments = assignments.Where(x => x.ClassroomId == classroomId).ToList();
        }

        var isTeacher = string.Equals(query.ViewerRole, nameof(UserRole.Teacher), StringComparison.OrdinalIgnoreCase);
        var isStudent = string.Equals(query.ViewerRole, nameof(UserRole.Student), StringComparison.OrdinalIgnoreCase);
        var isAdmin = string.Equals(query.ViewerRole, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase);

        if (isTeacher)
        {
            assignments = assignments.Where(x => x.Classroom?.Courses.Any(t => t.TeacherId == query.ViewerUserId) == true).ToList();
        }
        else if (isStudent)
        {
            assignments = assignments
                .Where(x => x.Classroom?.Students.Any(s => s.StudentId == query.ViewerUserId) == true)
                .ToList();
        }
        else if (!isAdmin)
        {
            assignments = [];
        }

        return assignments.Select(a => CreateAssignmentCommandHandler.Map(
            a,
            includeAnswerKey: isTeacher || isAdmin,
            includeSolutionVideo: isTeacher || isAdmin)).ToList();
    }
}
