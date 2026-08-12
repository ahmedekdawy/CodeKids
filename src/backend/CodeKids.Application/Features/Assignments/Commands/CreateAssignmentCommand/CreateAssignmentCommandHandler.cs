using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Assignments;

public sealed class CreateAssignmentCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateAssignmentCommand, AssignmentDto>
{
    public async Task<AssignmentDto> Handle(CreateAssignmentCommand command, CancellationToken cancellationToken)
    {
        var classroom = await dbContext.Classrooms
            .Include(x => x.Courses)
            .FirstOrDefaultAsync(x => x.Id == command.ClassroomId, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found.");

        if (!classroom.Courses.Any(t => t.TeacherId == command.TeacherUserId))
        {
            throw new InvalidOperationException("Only an assigned classroom teacher can create assignments.");
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

        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            ClassroomId = classroom.Id,
            CreatedByUserId = command.TeacherUserId,
            Title = title,
            Description = (command.Description ?? string.Empty).Trim(),
            DueAtUtc = command.DueAtUtc?.ToUniversalTime(),
            XpReward = Math.Max(0, command.XpReward),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var order = 1;
        foreach (var q in command.Questions)
        {
            if (!Enum.TryParse<AssignmentQuestionType>(q.QuestionType, true, out var type))
            {
                throw new InvalidOperationException("Question type must be ShortAnswer or MultipleChoice.");
            }

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
                SortOrder = q.SortOrder <= 0 ? order : q.SortOrder
            });
            order++;
        }

        dbContext.Assignments.Add(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await LoadAssignment(dbContext, assignment.Id, includeAnswerKey: true, cancellationToken))!;
    }

    internal static async Task<AssignmentDto?> LoadAssignment(
        IAppDbContext dbContext,
        Guid id,
        bool includeAnswerKey,
        CancellationToken cancellationToken)
    {
        var assignment = await dbContext.Assignments
            .AsNoTracking()
            .Include(x => x.Classroom)
                .ThenInclude(c => c!.Courses)
            .Include(x => x.CreatedBy)
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return assignment is null ? null : Map(assignment, includeAnswerKey, includeSolutionVideo: includeAnswerKey);
    }

    internal static AssignmentDto Map(Assignment assignment, bool includeAnswerKey, bool includeSolutionVideo) =>
        new(
            assignment.Id,
            assignment.ClassroomId,
            assignment.Classroom?.Name ?? "Classroom",
            assignment.Title,
            assignment.Description,
            assignment.DueAtUtc,
            assignment.XpReward,
            assignment.CreatedByUserId,
            assignment.CreatedBy?.DisplayName ?? "Teacher",
            includeSolutionVideo ? assignment.SolutionVideoMediaAssetId : null,
            assignment.Questions
                .OrderBy(x => x.SortOrder)
                .Select(q => new AssignmentQuestionDto(
                    q.Id,
                    q.Prompt,
                    q.QuestionType.ToString(),
                    q.OptionA,
                    q.OptionB,
                    q.OptionC,
                    q.Points,
                    q.SortOrder,
                    includeAnswerKey ? q.CorrectAnswer : null))
                .ToList());
}
