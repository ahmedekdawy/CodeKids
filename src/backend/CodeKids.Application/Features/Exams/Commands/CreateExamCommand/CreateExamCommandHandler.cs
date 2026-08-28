using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Application.Features.QuestionImages;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Exams;

public sealed class CreateExamCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateExamCommand, ExamDto>
{
    public async Task<ExamDto> Handle(CreateExamCommand command, CancellationToken cancellationToken)
    {
        var classroom = await dbContext.Classrooms
            .Include(x => x.Courses)
            .FirstOrDefaultAsync(x => x.Id == command.ClassroomId, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found.");

        if (!classroom.Courses.Any(t => t.TeacherId == command.TeacherUserId))
        {
            throw new InvalidOperationException("Only an assigned classroom teacher can create exams.");
        }

        var title = command.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Exam title is required.");
        }

        if (command.QuestionIds is null || command.QuestionIds.Count == 0)
        {
            throw new InvalidOperationException("Select at least one bank question.");
        }

        var courseId = command.CourseId ?? classroom.CourseId;
        if (courseId is Guid cid)
        {
            _ = await dbContext.Courses.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == cid, cancellationToken)
                ?? throw new InvalidOperationException("Course not found.");
        }

        var bankRoots = await dbContext.BankQuestions
            .Include(x => x.Children)
            .Where(x => command.QuestionIds.Contains(x.Id) && x.ParentQuestionId == null)
            .ToListAsync(cancellationToken);

        if (bankRoots.Count != command.QuestionIds.Distinct().Count())
        {
            throw new InvalidOperationException("One or more question IDs were not found (use parent question IDs only).");
        }

        foreach (var q in bankRoots)
        {
            if (q.CreatedByUserId != command.TeacherUserId)
            {
                throw new InvalidOperationException("You can only use your own bank questions in an exam.");
            }

            if (courseId is Guid examCourse && q.CourseId != examCourse)
            {
                throw new InvalidOperationException($"Question '{q.Prompt}' belongs to a different course.");
            }
        }

        var exam = new Exam
        {
            Id = Guid.NewGuid(),
            ClassroomId = classroom.Id,
            CourseId = courseId,
            CreatedByUserId = command.TeacherUserId,
            Title = title,
            Description = (command.Description ?? string.Empty).Trim(),
            DueAtUtc = command.DueAtUtc?.ToUniversalTime(),
            XpReward = Math.Max(0, command.XpReward),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var sort = 1;
        foreach (var bankId in command.QuestionIds.Distinct())
        {
            var bank = bankRoots.First(x => x.Id == bankId);
            var parentExamQuestion = Snapshot(exam.Id, bank, parentExamQuestionId: null, sort++);
            exam.Questions.Add(parentExamQuestion);

            foreach (var child in bank.Children.OrderBy(x => x.SortOrder))
            {
                exam.Questions.Add(Snapshot(exam.Id, child, parentExamQuestion.Id, child.SortOrder));
            }
        }

        dbContext.Exams.Add(exam);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await LoadExam(dbContext, exam.Id, includeAnswerKey: true, cancellationToken))!;
    }

    private static ExamQuestion Snapshot(Guid examId, BankQuestion bank, Guid? parentExamQuestionId, int sortOrder) =>
        new()
        {
            Id = Guid.NewGuid(),
            ExamId = examId,
            BankQuestionId = bank.Id,
            ParentExamQuestionId = parentExamQuestionId,
            LessonId = bank.LessonId,
            QuestionType = bank.QuestionType,
            Prompt = bank.Prompt,
            PassageText = bank.PassageText,
            OptionA = bank.OptionA,
            OptionB = bank.OptionB,
            OptionC = bank.OptionC,
            OptionD = bank.OptionD,
            OptionsJson = string.IsNullOrWhiteSpace(bank.OptionsJson) ? "[]" : bank.OptionsJson,
            CorrectAnswer = bank.CorrectAnswer,
            Points = bank.Points,
            SortOrder = sortOrder,
            PromptImageMediaAssetId = bank.PromptImageMediaAssetId
        };

    internal static async Task<ExamDto?> LoadExam(
        IAppDbContext dbContext,
        Guid id,
        bool includeAnswerKey,
        CancellationToken cancellationToken)
    {
        var exam = await dbContext.Exams
            .AsNoTracking()
            .Include(x => x.Classroom)
                .ThenInclude(c => c!.Courses)
            .Include(x => x.Course)
            .Include(x => x.CreatedBy)
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return exam is null ? null : Map(exam, includeAnswerKey);
    }

    internal static ExamDto Map(Exam exam, bool includeAnswerKey)
    {
        var roots = exam.Questions
            .Where(x => x.ParentExamQuestionId is null)
            .OrderBy(x => x.SortOrder)
            .Select(q => MapQuestion(q, exam.Questions, includeAnswerKey))
            .ToList();

        return new ExamDto(
            exam.Id,
            exam.ClassroomId,
            exam.Classroom?.Name ?? "Classroom",
            exam.CourseId,
            exam.Course?.Title,
            exam.Title,
            exam.Description,
            exam.DueAtUtc,
            exam.XpReward,
            exam.CreatedByUserId,
            exam.CreatedBy?.DisplayName ?? "Teacher",
            roots);
    }

    private static ExamQuestionDto MapQuestion(
        ExamQuestion q,
        IEnumerable<ExamQuestion> all,
        bool includeAnswerKey) =>
        new(
            q.Id,
            q.BankQuestionId,
            q.ParentExamQuestionId,
            q.QuestionType.ToString(),
            q.Prompt,
            q.PassageText,
            q.OptionA,
            q.OptionB,
            q.OptionC,
            q.OptionD,
            ChoiceOptions.Parse(q.OptionsJson, q.OptionA, q.OptionB, q.OptionC, q.OptionD),
            q.Points,
            q.SortOrder,
            includeAnswerKey ? q.CorrectAnswer : null,
            QuestionImageUrls.Build(q.PromptImageMediaAssetId),
            all.Where(c => c.ParentExamQuestionId == q.Id)
                .OrderBy(c => c.SortOrder)
                .Select(c => MapQuestion(c, all, includeAnswerKey))
                .ToList());
}
