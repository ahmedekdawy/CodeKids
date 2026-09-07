using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Assessments;
using CodeKids.Application.Features.Badges;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Assignments;

public sealed class GetAssignmentByIdQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetAssignmentByIdQuery, AssignmentDto?>
{
    public async Task<AssignmentDto?> Handle(GetAssignmentByIdQuery query, CancellationToken cancellationToken)
    {
        var includeKey = PublishedAssessmentAccess.CanViewUnpublished(query.ViewerRole);

        var assignment = await dbContext.Assignments
            .AsNoTracking()
            .Include(x => x.Classroom)
                .ThenInclude(c => c!.Courses)
            .Include(x => x.Classroom)
                .ThenInclude(c => c!.Students)
            .Include(x => x.CreatedBy)
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == query.AssignmentId, cancellationToken);

        if (assignment is null)
        {
            return null;
        }

        var isStudent = string.Equals(query.ViewerRole, nameof(UserRole.Student), StringComparison.OrdinalIgnoreCase);
        if (!includeKey && !assignment.IsPublished)
        {
            return null;
        }

        if (isStudent)
        {
            var enrolled = assignment.Classroom?.Students.Any(s => s.StudentId == query.ViewerUserId) == true;
            if (!enrolled)
            {
                return null;
            }
        }

        return CreateAssignmentCommandHandler.Map(assignment, includeKey, includeSolutionVideo: includeKey);
    }
}
