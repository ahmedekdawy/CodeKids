using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.QuestionImages;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Assignments;

public sealed class UpdateAssignmentCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<UpdateAssignmentCommand, AssignmentDto>
{
    public async Task<AssignmentDto> Handle(UpdateAssignmentCommand command, CancellationToken cancellationToken)
    {
        var assignment = await dbContext.Assignments
            .Include(x => x.Questions)
            .Include(x => x.Submissions)
                .ThenInclude(x => x.Answers)
            .FirstOrDefaultAsync(x => x.Id == command.AssignmentId, cancellationToken)
            ?? throw new InvalidOperationException("Assignment not found.");

        await AssignmentAuthorization.EnsureCanManageClassroomAsync(
            dbContext, command.TeacherUserId, assignment.ClassroomId, "edit", cancellationToken);

        if (command.ClassroomId != assignment.ClassroomId)
        {
            var classroomExists = await dbContext.Classrooms.AnyAsync(x => x.Id == command.ClassroomId, cancellationToken);
            if (!classroomExists)
            {
                throw new InvalidOperationException("Classroom not found.");
            }

            await AssignmentAuthorization.EnsureCanManageClassroomAsync(
                dbContext, command.TeacherUserId, command.ClassroomId, "edit", cancellationToken);
            assignment.ClassroomId = command.ClassroomId;
        }

        var title = command.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Assignment title is required.");
        }

        if (command.Questions is null || command.Questions.Count == 0)
        {
            throw new InvalidOperationException("Add at least one question.");
        }

        assignment.Title = title;
        assignment.Description = (command.Description ?? string.Empty).Trim();
        assignment.DueAtUtc = command.DueAtUtc?.ToUniversalTime();
        assignment.XpReward = Math.Max(0, command.XpReward);

        var incoming = command.Questions.ToList();
        var keptIds = incoming
            .Where(q => q.Id is Guid id && assignment.Questions.Any(existing => existing.Id == id))
            .Select(q => q.Id!.Value)
            .ToHashSet();

        var removed = assignment.Questions.Where(q => !keptIds.Contains(q.Id)).ToList();
        if (removed.Count > 0)
        {
            var removedIds = removed.Select(q => q.Id).ToHashSet();
            var answers = assignment.Submissions
                .SelectMany(s => s.Answers)
                .Where(a => removedIds.Contains(a.QuestionId))
                .ToList();
            dbContext.AssignmentAnswers.RemoveRange(answers);
            dbContext.AssignmentQuestions.RemoveRange(removed);
        }

        var order = 1;
        foreach (var q in incoming)
        {
            if (!Enum.TryParse<AssignmentQuestionType>(q.QuestionType, true, out var type))
            {
                throw new InvalidOperationException("Question type must be ShortAnswer or MultipleChoice.");
            }

            await QuestionImageAssetValidator.EnsureExistsAsync(dbContext, q.PromptImageMediaAssetId, cancellationToken);

            var existing = q.Id is Guid id && keptIds.Contains(id)
                ? assignment.Questions.FirstOrDefault(x => x.Id == id)
                : null;
            if (existing is null)
            {
                assignment.Questions.Add(new AssignmentQuestion
                {
                    Id = Guid.NewGuid(),
                    AssignmentId = assignment.Id,
                    Prompt = q.Prompt.Trim(),
                    QuestionType = type,
                    OptionA = q.OptionA,
                    OptionB = q.OptionB,
                    OptionC = q.OptionC,
                    CorrectAnswer = q.CorrectAnswer.Trim(),
                    Points = q.Points <= 0 ? 1 : q.Points,
                    SortOrder = q.SortOrder <= 0 ? order : q.SortOrder,
                    PromptImageMediaAssetId = q.PromptImageMediaAssetId
                });
            }
            else
            {
                existing.Prompt = q.Prompt.Trim();
                existing.QuestionType = type;
                existing.OptionA = q.OptionA;
                existing.OptionB = q.OptionB;
                existing.OptionC = q.OptionC;
                existing.CorrectAnswer = q.CorrectAnswer.Trim();
                existing.Points = q.Points <= 0 ? 1 : q.Points;
                existing.SortOrder = q.SortOrder <= 0 ? order : q.SortOrder;
                existing.PromptImageMediaAssetId = q.PromptImageMediaAssetId ?? existing.PromptImageMediaAssetId;
            }

            order++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await CreateAssignmentCommandHandler.LoadAssignment(
            dbContext, assignment.Id, includeAnswerKey: true, cancellationToken))!;
    }
}
