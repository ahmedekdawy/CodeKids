using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Quizzes;

public sealed record QuizQuestionDto(
    Guid Id,
    string Prompt,
    string OptionA,
    string OptionB,
    string OptionC,
    IReadOnlyList<ChoiceOptionDto> Options,
    int SortOrder);

public sealed record QuizDto(
    Guid Id,
    Guid CourseId,
    Guid? ClassroomId,
    string Title,
    string Description,
    int XpReward,
    IReadOnlyList<QuizQuestionDto> Questions);

public sealed record QuizAnswerDto(Guid QuestionId, string SelectedOption);

public sealed record SubmitQuizRequest(Guid QuizId, IReadOnlyList<QuizAnswerDto> Answers);

public sealed record SubmitQuizResponse(
    int Score,
    int TotalQuestions,
    int EarnedXp,
    int TotalXp,
    string Feedback,
    string? FeedbackCode,
    IReadOnlyList<string> NewlyAwardedBadges);

public sealed record CreateQuizQuestionInput(
    string Prompt,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    IReadOnlyList<string>? Options,
    string CorrectOption,
    int SortOrder);

public sealed record CreateQuizRequest(
    Guid CourseId,
    Guid? ClassroomId,
    string Title,
    string? Description,
    int XpReward,
    IReadOnlyList<CreateQuizQuestionInput> Questions);

public sealed record CreateQuizCommand(
    Guid TeacherUserId,
    Guid CourseId,
    Guid? ClassroomId,
    string Title,
    string? Description,
    int XpReward,
    IReadOnlyList<CreateQuizQuestionInput> Questions) : ICommand<QuizDto>;

public sealed record GetQuizzesQuery(Guid? CourseId = null, Guid? ClassroomId = null) : IQuery<IReadOnlyList<QuizDto>>;

public sealed record GetQuizByIdQuery(Guid QuizId) : IQuery<QuizDto?>;

public sealed record SubmitQuizCommand(
    Guid UserId,
    Guid QuizId,
    IReadOnlyList<QuizAnswerDto> Answers) : ICommand<SubmitQuizResponse>;

public sealed class CreateQuizCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateQuizCommand, QuizDto>
{
    public async Task<QuizDto> Handle(CreateQuizCommand command, CancellationToken cancellationToken)
    {
        var courseExists = await dbContext.Courses.AnyAsync(x => x.Id == command.CourseId, cancellationToken);
        if (!courseExists)
        {
            throw new InvalidOperationException("Course not found.");
        }

        if (command.ClassroomId is Guid classroomId)
        {
            var classroom = await dbContext.Classrooms.FirstOrDefaultAsync(x => x.Id == classroomId, cancellationToken)
                ?? throw new InvalidOperationException("Classroom not found.");
            if (classroom.TeacherId != command.TeacherUserId)
            {
                throw new InvalidOperationException("Only the assigned classroom teacher can create quizzes for that classroom.");
            }
        }
        else
        {
            var teaches = await dbContext.Classrooms.AnyAsync(x => x.TeacherId == command.TeacherUserId, cancellationToken);
            if (!teaches)
            {
                throw new InvalidOperationException("Teacher must be assigned to a classroom before creating quizzes.");
            }
        }

        if (command.Questions is null || command.Questions.Count == 0)
        {
            throw new InvalidOperationException("Add at least one question.");
        }

        var quiz = new Quiz
        {
            Id = Guid.NewGuid(),
            CourseId = command.CourseId,
            ClassroomId = command.ClassroomId,
            CreatedByUserId = command.TeacherUserId,
            Title = command.Title.Trim(),
            Description = (command.Description ?? string.Empty).Trim(),
            XpReward = Math.Max(5, command.XpReward)
        };

        var order = 1;
        foreach (var q in command.Questions)
        {
            var options = q.Options is { Count: > 0 }
                ? ChoiceOptions.FromTexts(q.Options)
                : ChoiceOptions.Parse(null, q.OptionA, q.OptionB, q.OptionC);

            if (options.Count < 2)
            {
                throw new InvalidOperationException("Each quiz question needs at least two options.");
            }

            var correct = q.CorrectOption.Trim().ToUpperInvariant();
            if (!ChoiceOptions.AllowedKeys(options).Contains(correct))
            {
                throw new InvalidOperationException("Correct option must match one of the listed choices.");
            }

            var (a, b, c, _) = ChoiceOptions.ToLegacy(options);
            quiz.Questions.Add(new QuizQuestion
            {
                Id = Guid.NewGuid(),
                QuizId = quiz.Id,
                Prompt = q.Prompt.Trim(),
                OptionA = a ?? string.Empty,
                OptionB = b ?? string.Empty,
                OptionC = c ?? string.Empty,
                OptionsJson = ChoiceOptions.ToJson(options),
                CorrectOption = correct,
                SortOrder = q.SortOrder <= 0 ? order : q.SortOrder
            });
            order++;
        }

        dbContext.Quizzes.Add(quiz);
        await dbContext.SaveChangesAsync(cancellationToken);
        return GetQuizzesQueryHandler.Map(quiz);
    }
}

public sealed class GetQuizzesQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetQuizzesQuery, IReadOnlyList<QuizDto>>
{
    public async Task<IReadOnlyList<QuizDto>> Handle(GetQuizzesQuery query, CancellationToken cancellationToken)
    {
        var quizzesQuery = dbContext.Quizzes
            .AsNoTracking()
            .Include(x => x.Questions)
            .AsQueryable();

        if (query.CourseId is Guid courseId)
        {
            quizzesQuery = quizzesQuery.Where(x => x.CourseId == courseId);
        }

        if (query.ClassroomId is Guid classroomId)
        {
            quizzesQuery = quizzesQuery.Where(x => x.ClassroomId == null || x.ClassroomId == classroomId);
        }

        var quizzes = await quizzesQuery.ToListAsync(cancellationToken);
        return quizzes.Select(Map).ToList();
    }

    internal static QuizDto Map(Quiz quiz) =>
        new(
            quiz.Id,
            quiz.CourseId,
            quiz.ClassroomId,
            quiz.Title,
            quiz.Description,
            quiz.XpReward,
            quiz.Questions
                .OrderBy(x => x.SortOrder)
                .Select(x => new QuizQuestionDto(
                    x.Id,
                    x.Prompt,
                    x.OptionA,
                    x.OptionB,
                    x.OptionC,
                    ChoiceOptions.Parse(x.OptionsJson, x.OptionA, x.OptionB, x.OptionC),
                    x.SortOrder))
                .ToList());
}

public sealed class GetQuizByIdQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetQuizByIdQuery, QuizDto?>
{
    public async Task<QuizDto?> Handle(GetQuizByIdQuery query, CancellationToken cancellationToken)
    {
        var quiz = await dbContext.Quizzes
            .AsNoTracking()
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == query.QuizId, cancellationToken);

        return quiz is null ? null : GetQuizzesQueryHandler.Map(quiz);
    }
}

public sealed class SubmitQuizCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<SubmitQuizCommand, SubmitQuizResponse>
{
    public async Task<SubmitQuizResponse> Handle(SubmitQuizCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Student not found.");

        var quiz = await dbContext.Quizzes
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == command.QuizId, cancellationToken)
            ?? throw new InvalidOperationException("Quiz not found.");

        var score = 0;
        foreach (var question in quiz.Questions)
        {
            var answer = command.Answers.FirstOrDefault(x => x.QuestionId == question.Id);
            if (answer is not null &&
                string.Equals(answer.SelectedOption, question.CorrectOption, StringComparison.OrdinalIgnoreCase))
            {
                score++;
            }
        }

        var total = quiz.Questions.Count;
        var ratio = total == 0 ? 0 : (double)score / total;
        var earnedXp = ratio >= 0.7 ? quiz.XpReward : Math.Max(5, quiz.XpReward / 3);

        dbContext.QuizAttempts.Add(new QuizAttempt
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            QuizId = quiz.Id,
            Score = score,
            TotalQuestions = total,
            EarnedXp = earnedXp,
            CompletedAtUtc = DateTimeOffset.UtcNow
        });

        user.TotalXp += earnedXp;
        await dbContext.SaveChangesAsync(cancellationToken);

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

        return new SubmitQuizResponse(
            score,
            total,
            earnedXp,
            user.TotalXp,
            ratio >= 0.7 ? "Quiz cleared! Your coding brain is glowing." : "Keep practicing — you're getting closer!",
            ratio >= 0.7 ? "api.feedback.quizPassed" : "api.feedback.quizRetry",
            newBadges);
    }
}

