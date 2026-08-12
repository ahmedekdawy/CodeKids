using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Progress;

public sealed class CompleteStepCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CompleteStepCommand, CompleteStepResponse>
{
    public async Task<CompleteStepResponse> Handle(CompleteStepCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Student not found.");

        var step = await dbContext.LessonSteps
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == command.StepId && x.LessonId == command.LessonId,
                cancellationToken);

        if (step is null)
        {
            return new CompleteStepResponse(false, 0, "We couldn't find that challenge step.", "api.feedback.stepNotFound", user.TotalXp, []);
        }

        var alreadyDone = await dbContext.StudentProgress.AnyAsync(
            x => x.UserId == command.UserId && x.StepId == command.StepId && x.IsCompleted,
            cancellationToken);

        var isCorrect = Normalize(step.ExpectedAnswer) == Normalize(command.SubmittedAnswer);
        if (!isCorrect)
        {
            return new CompleteStepResponse(false, 0, "Not quite yet. Try matching the example carefully.", "api.feedback.stepIncorrect", user.TotalXp, []);
        }

        var earnedXp = alreadyDone ? 0 : 20;
        if (!alreadyDone)
        {
            dbContext.StudentProgress.Add(new StudentProgress
            {
                Id = Guid.NewGuid(),
                UserId = command.UserId,
                LessonId = command.LessonId,
                StepId = command.StepId,
                IsCompleted = true,
                EarnedXp = earnedXp,
                CompletedAtUtc = DateTimeOffset.UtcNow
            });
            user.TotalXp += earnedXp;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var beforeBadges = await dbContext.UserBadges
            .Where(x => x.UserId == user.Id)
            .Select(x => x.BadgeId)
            .ToListAsync(cancellationToken);

        await BadgeAwarder.AwardEligibleAsync(dbContext, user, cancellationToken);

        var newBadges = await dbContext.UserBadges
            .Include(x => x.Badge)
            .Where(x => x.UserId == user.Id && !beforeBadges.Contains(x.BadgeId))
            .Select(x => x.Badge!.Name)
            .ToListAsync(cancellationToken);

        return new CompleteStepResponse(
            true,
            earnedXp,
            alreadyDone ? "You already mastered this step. Nice review!" : "Great job! You solved the coding step.",
            alreadyDone ? "api.feedback.stepAlreadyDone" : "api.feedback.stepCorrect",
            user.TotalXp,
            newBadges);
    }

    private static string Normalize(string value) =>
        value.Trim().Replace(" ", string.Empty).ToUpperInvariant();
}
