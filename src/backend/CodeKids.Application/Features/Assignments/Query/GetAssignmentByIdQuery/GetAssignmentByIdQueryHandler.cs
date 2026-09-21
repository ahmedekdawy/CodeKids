using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Assessments;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.Classrooms;
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
            .Include(x => x.Course)
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
            var courseIds = await StudentCourseVisibility.GetAssessmentCourseIdsAsync(
                dbContext, query.ViewerUserId, cancellationToken);
            var enrolled = (assignment.CourseId is Guid cid && courseIds.Contains(cid))
                           || assignment.Classroom?.Students.Any(s => s.StudentId == query.ViewerUserId) == true;
            if (!enrolled)
            {
                return null;
            }
        }
        else if (!TeacherAssessmentAccess.CanViewAsStaff(query.ViewerRole, assignment.CreatedByUserId, query.ViewerUserId))
        {
            return null;
        }

        var dto = CreateAssignmentCommandHandler.Map(assignment, includeKey, includeSolutionVideo: includeKey);
        if (isStudent && await StudentCompletedAssessments.HasAssignmentAsync(
                dbContext, query.ViewerUserId, assignment.Id, cancellationToken))
        {
            return dto with { AlreadySubmitted = true };
        }

        return dto;
    }
}
