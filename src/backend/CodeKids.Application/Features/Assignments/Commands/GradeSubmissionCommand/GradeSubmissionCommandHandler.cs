using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Assignments;

public sealed class GradeSubmissionCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<GradeSubmissionCommand, AssignmentSubmissionDto>
{
    public async Task<AssignmentSubmissionDto> Handle(GradeSubmissionCommand command, CancellationToken cancellationToken)
    {
        var submission = await dbContext.AssignmentSubmissions
            .Include(x => x.Answers)
            .Include(x => x.Assignment!)
                .ThenInclude(a => a.Classroom!)
                .ThenInclude(c => c.Courses)
            .Include(x => x.Assignment!)
                .ThenInclude(a => a.Questions)
            .FirstOrDefaultAsync(x => x.Id == command.SubmissionId, cancellationToken)
            ?? throw new InvalidOperationException("Submission not found.");

        if (submission.Assignment?.Classroom?.Courses.Any(t => t.TeacherId == command.TeacherUserId) != true)
        {
            throw new InvalidOperationException("Only the classroom teacher can grade submissions.");
        }

        if (command.Answers is not null)
        {
            foreach (var grade in command.Answers)
            {
                var answer = submission.Answers.FirstOrDefault(x => x.QuestionId == grade.QuestionId);
                if (answer is null) continue;
                answer.IsCorrect = grade.IsCorrect;
                answer.PointsAwarded = Math.Max(0, grade.PointsAwarded);
            }
        }

        var wasAlreadyGraded = submission.Status == AssignmentSubmissionStatus.Graded;

        submission.Score = submission.Answers.Sum(x => x.PointsAwarded ?? 0);
        submission.MaxScore = submission.Assignment.Questions.Sum(x => x.Points);
        submission.TeacherFeedback = command.TeacherFeedback?.Trim();
        submission.Status = AssignmentSubmissionStatus.Graded;
        submission.GradedAtUtc = DateTimeOffset.UtcNow;

        var student = await dbContext.Users.FirstAsync(x => x.Id == submission.StudentId, cancellationToken);
        if (!wasAlreadyGraded &&
            submission.MaxScore > 0 &&
            submission.Score >= Math.Ceiling(submission.MaxScore.Value * 0.7))
        {
            student.TotalXp += submission.Assignment.XpReward;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await BadgeAwarder.AwardEligibleAsync(dbContext, student, cancellationToken);

        return (await SubmitAssignmentCommandHandler.LoadSubmission(dbContext, submission.Id, cancellationToken))!;
    }
}
