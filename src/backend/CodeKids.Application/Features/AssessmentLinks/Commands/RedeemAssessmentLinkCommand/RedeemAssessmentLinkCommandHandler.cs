using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Auth;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.AssessmentLinks;

public sealed class RedeemAssessmentLinkCommandHandler(
    IAppDbContext dbContext,
    IAssessmentAccessTokenService tokenService,
    IJwtTokenService jwtTokenService)
    : ICommandHandler<RedeemAssessmentLinkCommand, RedeemAssessmentLinkResult>
{
    public async Task<RedeemAssessmentLinkResult> Handle(
        RedeemAssessmentLinkCommand command,
        CancellationToken cancellationToken)
    {
        if (!tokenService.TryValidate(
                command.Key,
                out var kind,
                out var resourceId,
                out var studentId,
                out _))
        {
            throw new InvalidOperationException("This assessment link is invalid or has expired.");
        }

        var student = await dbContext.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == studentId, cancellationToken)
            ?? throw new InvalidOperationException("This assessment link is invalid or has expired.");

        if (student.Role != UserRole.Student || !student.IsActive)
        {
            throw new InvalidOperationException("This assessment link is invalid or has expired.");
        }

        Guid? classroomId;
        bool isPublished;
        string redirectPath;

        switch (kind)
        {
            case AssessmentLinkKind.Exam:
            {
                var exam = await dbContext.Exams.IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == resourceId, cancellationToken)
                    ?? throw new InvalidOperationException("This assessment is no longer available.");
                classroomId = exam.ClassroomId;
                isPublished = exam.IsPublished;
                redirectPath = $"/exams/{exam.Id:D}";
                break;
            }
            case AssessmentLinkKind.Quiz:
            {
                var quiz = await dbContext.Quizzes.IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == resourceId, cancellationToken)
                    ?? throw new InvalidOperationException("This assessment is no longer available.");
                classroomId = quiz.ClassroomId;
                isPublished = quiz.IsPublished;
                redirectPath = $"/quizzes/{quiz.Id:D}";
                break;
            }
            case AssessmentLinkKind.Assignment:
            {
                var assignment = await dbContext.Assignments.IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == resourceId, cancellationToken)
                    ?? throw new InvalidOperationException("This assessment is no longer available.");
                classroomId = assignment.ClassroomId;
                isPublished = assignment.IsPublished;
                redirectPath = $"/assignments/{assignment.Id:D}";
                break;
            }
            default:
                throw new InvalidOperationException("This assessment link is invalid or has expired.");
        }

        if (!isPublished)
        {
            throw new InvalidOperationException("This assessment is not available yet.");
        }

        if (classroomId is Guid roomId)
        {
            var enrolled = await dbContext.ClassroomStudents.IgnoreQueryFilters()
                .AnyAsync(
                    x => x.ClassroomId == roomId && x.StudentId == student.Id,
                    cancellationToken);
            if (!enrolled)
            {
                throw new InvalidOperationException("You are not enrolled for this assessment.");
            }
        }

        student.LastLoginDateUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RedeemAssessmentLinkResult(
            jwtTokenService.CreateToken(student),
            RegisterCommandHandler.ToDto(student),
            redirectPath,
            kind.ToString().ToLowerInvariant(),
            resourceId);
    }
}
