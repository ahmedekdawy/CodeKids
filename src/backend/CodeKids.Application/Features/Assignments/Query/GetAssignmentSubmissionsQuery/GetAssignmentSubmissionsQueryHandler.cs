using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Assignments;

public sealed class GetAssignmentSubmissionsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetAssignmentSubmissionsQuery, IReadOnlyList<AssignmentSubmissionDto>>
{
    public async Task<IReadOnlyList<AssignmentSubmissionDto>> Handle(
        GetAssignmentSubmissionsQuery query,
        CancellationToken cancellationToken)
    {
        var assignment = await dbContext.Assignments
            .Include(x => x.Classroom)
                .ThenInclude(c => c!.Courses)
            .FirstOrDefaultAsync(x => x.Id == query.AssignmentId, cancellationToken)
            ?? throw new InvalidOperationException("Assignment not found.");

        if (assignment.Classroom?.Courses.Any(t => t.TeacherId == query.TeacherUserId) != true)
        {
            throw new InvalidOperationException("Only the classroom teacher can review submissions.");
        }

        var submissions = await dbContext.AssignmentSubmissions
            .AsNoTracking()
            .Include(x => x.Student)
            .Include(x => x.Assignment)
            .Include(x => x.Answers)
                .ThenInclude(a => a.Question)
            .Where(x => x.AssignmentId == query.AssignmentId)
            .OrderByDescending(x => x.SubmittedAtUtc)
            .ToListAsync(cancellationToken);

        return submissions.Select(SubmitAssignmentCommandHandler.MapSubmission).ToList();
    }
}
