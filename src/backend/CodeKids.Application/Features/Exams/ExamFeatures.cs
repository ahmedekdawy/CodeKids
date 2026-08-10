using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Exams;

public sealed record CreateExamRequest(
    Guid ClassroomId,
    Guid? CourseId,
    string Title,
    string? Description,
    DateTimeOffset? DueAtUtc,
    int XpReward,
    IReadOnlyList<Guid> QuestionIds);

public sealed record CreateExamCommand(
    Guid TeacherUserId,
    Guid ClassroomId,
    Guid? CourseId,
    string Title,
    string? Description,
    DateTimeOffset? DueAtUtc,
    int XpReward,
    IReadOnlyList<Guid> QuestionIds) : ICommand<ExamDto>;

public sealed record GetExamsQuery(Guid ViewerUserId, string ViewerRole, Guid? ClassroomId = null)
    : IQuery<IReadOnlyList<ExamDto>>;

public sealed record GetExamByIdQuery(Guid ExamId, Guid ViewerUserId, string ViewerRole) : IQuery<ExamDto?>;

public sealed record ExamAnswerInput(Guid QuestionId, string AnswerText);
public sealed record SubmitExamRequest(Guid ExamId, IReadOnlyList<ExamAnswerInput> Answers);
public sealed record SubmitExamCommand(
    Guid StudentId,
    Guid ExamId,
    IReadOnlyList<ExamAnswerInput> Answers) : ICommand<ExamAttemptDto>;

public sealed record StartExamCommand(Guid StudentId, Guid ExamId) : ICommand<ExamAttemptDto>;

public sealed record GetExamAttemptsQuery(Guid TeacherUserId, Guid ExamId) : IQuery<IReadOnlyList<ExamAttemptDto>>;

public sealed record ExamQuestionDto(
    Guid Id,
    Guid? BankQuestionId,
    Guid? ParentExamQuestionId,
    string QuestionType,
    string Prompt,
    string PassageText,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    string? OptionD,
    IReadOnlyList<ChoiceOptionDto> Options,
    int Points,
    int SortOrder,
    string? CorrectAnswer,
    IReadOnlyList<ExamQuestionDto> Children);

public sealed record ExamDto(
    Guid Id,
    Guid ClassroomId,
    string ClassroomName,
    Guid? CourseId,
    string? CourseTitle,
    string Title,
    string Description,
    DateTimeOffset? DueAtUtc,
    int XpReward,
    Guid CreatedByUserId,
    string CreatedByName,
    IReadOnlyList<ExamQuestionDto> Questions);

public sealed record ExamAnswerReviewDto(
    Guid QuestionId,
    string Prompt,
    string QuestionType,
    string AnswerText,
    string? CorrectAnswer,
    bool? IsCorrect,
    int? PointsAwarded,
    int Points);

public sealed record ExamAttemptDto(
    Guid Id,
    Guid ExamId,
    string ExamTitle,
    Guid StudentId,
    string StudentName,
    string Status,
    int? Score,
    int? MaxScore,
    string? TeacherFeedback,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? GradedAtUtc,
    int? DurationSeconds,
    IReadOnlyList<ExamAnswerReviewDto> Answers);

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
            SortOrder = sortOrder
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
            all.Where(c => c.ParentExamQuestionId == q.Id)
                .OrderBy(c => c.SortOrder)
                .Select(c => MapQuestion(c, all, includeAnswerKey))
                .ToList());
}

public sealed class GetExamsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetExamsQuery, IReadOnlyList<ExamDto>>
{
    public async Task<IReadOnlyList<ExamDto>> Handle(GetExamsQuery query, CancellationToken cancellationToken)
    {
        var exams = await dbContext.Exams
            .AsNoTracking()
            .Include(x => x.Classroom!)
                .ThenInclude(c => c.Courses)
            .Include(x => x.Classroom!)
                .ThenInclude(c => c.Students)
            .Include(x => x.Course)
            .Include(x => x.CreatedBy)
            .Include(x => x.Questions)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (query.ClassroomId is Guid classroomId)
        {
            exams = exams.Where(x => x.ClassroomId == classroomId).ToList();
        }

        var isTeacher = string.Equals(query.ViewerRole, nameof(UserRole.Teacher), StringComparison.OrdinalIgnoreCase);
        var isStudent = string.Equals(query.ViewerRole, nameof(UserRole.Student), StringComparison.OrdinalIgnoreCase);
        var isAdmin = string.Equals(query.ViewerRole, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase);

        if (isTeacher)
        {
            exams = exams.Where(x => x.Classroom?.Courses.Any(t => t.TeacherId == query.ViewerUserId) == true).ToList();
        }
        else if (isStudent)
        {
            exams = exams
                .Where(x => x.Classroom?.Students.Any(s => s.StudentId == query.ViewerUserId) == true)
                .ToList();
        }
        else if (!isAdmin)
        {
            exams = [];
        }

        return exams.Select(e => CreateExamCommandHandler.Map(e, includeAnswerKey: isTeacher || isAdmin)).ToList();
    }
}

public sealed class GetExamByIdQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetExamByIdQuery, ExamDto?>
{
    public async Task<ExamDto?> Handle(GetExamByIdQuery query, CancellationToken cancellationToken)
    {
        var includeKey = string.Equals(query.ViewerRole, nameof(UserRole.Teacher), StringComparison.OrdinalIgnoreCase)
            || string.Equals(query.ViewerRole, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase);
        return await CreateExamCommandHandler.LoadExam(dbContext, query.ExamId, includeKey, cancellationToken);
    }
}

public sealed class StartExamCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<StartExamCommand, ExamAttemptDto>
{
    public async Task<ExamAttemptDto> Handle(StartExamCommand command, CancellationToken cancellationToken)
    {
        var exam = await dbContext.Exams
            .Include(x => x.Classroom!)
                .ThenInclude(c => c.Courses)
            .Include(x => x.Classroom!)
                .ThenInclude(c => c.Students)
            .FirstOrDefaultAsync(x => x.Id == command.ExamId, cancellationToken)
            ?? throw new InvalidOperationException("Exam not found.");

        if (exam.Classroom?.Students.All(s => s.StudentId != command.StudentId) == true)
        {
            throw new InvalidOperationException("Student is not in this classroom.");
        }

        var existing = await dbContext.ExamAttempts
            .Include(x => x.Student)
            .Include(x => x.Exam)
            .Include(x => x.Answers)
                .ThenInclude(a => a.Question)
            .FirstOrDefaultAsync(
                x => x.ExamId == exam.Id && x.StudentId == command.StudentId,
                cancellationToken);

        if (existing is not null)
        {
            if (existing.Status != ExamAttemptStatus.InProgress)
            {
                throw new InvalidOperationException("Exam already submitted.");
            }

            return SubmitExamCommandHandler.MapAttempt(existing);
        }

        var student = await dbContext.Users.FirstAsync(x => x.Id == command.StudentId, cancellationToken);
        var attempt = new ExamAttempt
        {
            Id = Guid.NewGuid(),
            ExamId = exam.Id,
            StudentId = student.Id,
            Status = ExamAttemptStatus.InProgress,
            StartedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.ExamAttempts.Add(attempt);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await SubmitExamCommandHandler.LoadAttempt(dbContext, attempt.Id, cancellationToken))!;
    }
}

public sealed class SubmitExamCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<SubmitExamCommand, ExamAttemptDto>
{
    public async Task<ExamAttemptDto> Handle(SubmitExamCommand command, CancellationToken cancellationToken)
    {
        var exam = await dbContext.Exams
            .Include(x => x.Questions)
            .Include(x => x.Classroom!)
                .ThenInclude(c => c.Courses)
            .Include(x => x.Classroom!)
                .ThenInclude(c => c.Students)
            .FirstOrDefaultAsync(x => x.Id == command.ExamId, cancellationToken)
            ?? throw new InvalidOperationException("Exam not found.");

        if (exam.Classroom?.Students.All(s => s.StudentId != command.StudentId) == true)
        {
            throw new InvalidOperationException("Student is not in this classroom.");
        }

        var student = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.StudentId, cancellationToken)
            ?? throw new InvalidOperationException("Student not found.");

        var attempt = await dbContext.ExamAttempts
            .Include(x => x.Answers)
            .FirstOrDefaultAsync(
                x => x.ExamId == exam.Id && x.StudentId == command.StudentId,
                cancellationToken);

        if (attempt is not null && attempt.Status != ExamAttemptStatus.InProgress)
        {
            throw new InvalidOperationException("Exam already submitted.");
        }

        if (attempt is null)
        {
            attempt = new ExamAttempt
            {
                Id = Guid.NewGuid(),
                ExamId = exam.Id,
                StudentId = student.Id,
                Status = ExamAttemptStatus.InProgress,
                StartedAtUtc = DateTimeOffset.UtcNow
            };
            dbContext.ExamAttempts.Add(attempt);
        }

        attempt.Answers.Clear();
        attempt.Status = ExamAttemptStatus.Submitted;
        attempt.SubmittedAtUtc = DateTimeOffset.UtcNow;

        var autoScore = 0;
        var answerable = exam.Questions
            .Where(x => !BankQuestionValidator.IsComposite(x.QuestionType))
            .ToList();
        var maxScore = answerable.Sum(x => x.Points);
        var allAutoGradable = answerable.All(x => ExamGrading.IsAutoGradable(x.QuestionType));

        foreach (var question in answerable)
        {
            var input = command.Answers.FirstOrDefault(x => x.QuestionId == question.Id);
            var answerText = (input?.AnswerText ?? string.Empty).Trim();
            if (question.QuestionType == BankQuestionType.MultiChoice)
            {
                answerText = string.Join(',', ExamGrading.NormalizeMultiAnswer(answerText));
            }

            var isCorrect = ExamGrading.AnswersMatch(question.QuestionType, answerText, question.CorrectAnswer);
            var points = isCorrect ? question.Points : 0;
            if (isCorrect) autoScore += question.Points;

            attempt.Answers.Add(new ExamAnswer
            {
                Id = Guid.NewGuid(),
                AttemptId = attempt.Id,
                ExamQuestionId = question.Id,
                AnswerText = answerText,
                IsCorrect = isCorrect,
                PointsAwarded = points
            });
        }

        attempt.MaxScore = maxScore;
        if (allAutoGradable)
        {
            attempt.Score = autoScore;
            attempt.Status = ExamAttemptStatus.Graded;
            attempt.GradedAtUtc = DateTimeOffset.UtcNow;
            if (maxScore > 0 && autoScore >= Math.Ceiling(maxScore * 0.7))
            {
                student.TotalXp += exam.XpReward;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await BadgeAwarder.AwardEligibleAsync(dbContext, student, cancellationToken);
        return (await LoadAttempt(dbContext, attempt.Id, cancellationToken))!;
    }

    internal static async Task<ExamAttemptDto?> LoadAttempt(
        IAppDbContext dbContext,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        var attempt = await dbContext.ExamAttempts
            .AsNoTracking()
            .Include(x => x.Student)
            .Include(x => x.Exam)
            .Include(x => x.Answers)
                .ThenInclude(a => a.Question)
            .FirstOrDefaultAsync(x => x.Id == attemptId, cancellationToken);
        return attempt is null ? null : MapAttempt(attempt);
    }

    internal static ExamAttemptDto MapAttempt(ExamAttempt attempt) =>
        new(
            attempt.Id,
            attempt.ExamId,
            attempt.Exam?.Title ?? "Exam",
            attempt.StudentId,
            attempt.Student?.DisplayName ?? "Student",
            attempt.Status.ToString(),
            attempt.Score,
            attempt.MaxScore,
            attempt.TeacherFeedback,
            attempt.StartedAtUtc,
            attempt.SubmittedAtUtc,
            attempt.GradedAtUtc,
            attempt.DurationSeconds,
            attempt.Answers.Select(a => new ExamAnswerReviewDto(
                a.ExamQuestionId,
                a.Question?.Prompt ?? "",
                a.Question?.QuestionType.ToString() ?? "",
                a.AnswerText,
                a.Question?.CorrectAnswer,
                a.IsCorrect,
                a.PointsAwarded,
                a.Question?.Points ?? 0)).ToList());
}

public sealed class GetExamAttemptsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetExamAttemptsQuery, IReadOnlyList<ExamAttemptDto>>
{
    public async Task<IReadOnlyList<ExamAttemptDto>> Handle(
        GetExamAttemptsQuery query,
        CancellationToken cancellationToken)
    {
        var exam = await dbContext.Exams
            .Include(x => x.Classroom)
                .ThenInclude(c => c!.Courses)
            .FirstOrDefaultAsync(x => x.Id == query.ExamId, cancellationToken)
            ?? throw new InvalidOperationException("Exam not found.");

        if (exam.Classroom?.Courses.Any(t => t.TeacherId == query.TeacherUserId) != true)
        {
            throw new InvalidOperationException("Only the classroom teacher can review exam attempts.");
        }

        var attempts = await dbContext.ExamAttempts
            .AsNoTracking()
            .Include(x => x.Student)
            .Include(x => x.Exam)
            .Include(x => x.Answers)
                .ThenInclude(a => a.Question)
            .Where(x => x.ExamId == query.ExamId)
            .OrderByDescending(x => x.SubmittedAtUtc)
            .ToListAsync(cancellationToken);

        return attempts.Select(SubmitExamCommandHandler.MapAttempt).ToList();
    }
}
