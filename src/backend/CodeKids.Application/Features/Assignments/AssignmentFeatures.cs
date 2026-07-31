using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Assignments;

public sealed record AssignmentQuestionInput(
    string Prompt,
    string QuestionType,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    string CorrectAnswer,
    int Points,
    int SortOrder);

public sealed record AssignmentQuestionDto(
    Guid Id,
    string Prompt,
    string QuestionType,
    string? OptionA,
    string? OptionB,
    string? OptionC,
    int Points,
    int SortOrder,
    string? CorrectAnswer);

public sealed record AssignmentDto(
    Guid Id,
    Guid ClassroomId,
    string ClassroomName,
    string Title,
    string Description,
    DateTimeOffset? DueAtUtc,
    int XpReward,
    Guid CreatedByUserId,
    string CreatedByName,
    IReadOnlyList<AssignmentQuestionDto> Questions);

public sealed record CreateAssignmentRequest(
    Guid ClassroomId,
    string Title,
    string? Description,
    DateTimeOffset? DueAtUtc,
    int XpReward,
    IReadOnlyList<AssignmentQuestionInput> Questions);

public sealed record CreateAssignmentCommand(
    Guid TeacherUserId,
    Guid ClassroomId,
    string Title,
    string? Description,
    DateTimeOffset? DueAtUtc,
    int XpReward,
    IReadOnlyList<AssignmentQuestionInput> Questions) : ICommand<AssignmentDto>;

public sealed record GetAssignmentsQuery(Guid ViewerUserId, string ViewerRole, Guid? ClassroomId = null)
    : IQuery<IReadOnlyList<AssignmentDto>>;

public sealed record GetAssignmentByIdQuery(Guid AssignmentId, Guid ViewerUserId, string ViewerRole)
    : IQuery<AssignmentDto?>;

public sealed record AssignmentAnswerInput(Guid QuestionId, string AnswerText);

public sealed record SubmitAssignmentRequest(Guid AssignmentId, IReadOnlyList<AssignmentAnswerInput> Answers);

public sealed record SubmitAssignmentCommand(
    Guid StudentId,
    Guid AssignmentId,
    IReadOnlyList<AssignmentAnswerInput> Answers) : ICommand<AssignmentSubmissionDto>;

public sealed record AssignmentAnswerReviewDto(
    Guid QuestionId,
    string Prompt,
    string AnswerText,
    string? CorrectAnswer,
    bool? IsCorrect,
    int? PointsAwarded,
    int Points);

public sealed record AssignmentSubmissionDto(
    Guid Id,
    Guid AssignmentId,
    string AssignmentTitle,
    Guid StudentId,
    string StudentName,
    string Status,
    int? Score,
    int? MaxScore,
    string? TeacherFeedback,
    DateTimeOffset SubmittedAtUtc,
    DateTimeOffset? GradedAtUtc,
    IReadOnlyList<AssignmentAnswerReviewDto> Answers);

public sealed record GetAssignmentSubmissionsQuery(Guid TeacherUserId, Guid AssignmentId)
    : IQuery<IReadOnlyList<AssignmentSubmissionDto>>;

public sealed record GradeAnswerInput(Guid QuestionId, bool IsCorrect, int PointsAwarded);

public sealed record GradeSubmissionRequest(
    Guid SubmissionId,
    string? TeacherFeedback,
    IReadOnlyList<GradeAnswerInput>? Answers);

public sealed record GradeSubmissionCommand(
    Guid TeacherUserId,
    Guid SubmissionId,
    string? TeacherFeedback,
    IReadOnlyList<GradeAnswerInput>? Answers) : ICommand<AssignmentSubmissionDto>;

public sealed class CreateAssignmentCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateAssignmentCommand, AssignmentDto>
{
    public async Task<AssignmentDto> Handle(CreateAssignmentCommand command, CancellationToken cancellationToken)
    {
        var classroom = await dbContext.Classrooms
            .FirstOrDefaultAsync(x => x.Id == command.ClassroomId, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found.");

        if (classroom.TeacherId != command.TeacherUserId)
        {
            throw new InvalidOperationException("Only the assigned classroom teacher can create assignments.");
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
            .Include(x => x.CreatedBy)
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return assignment is null ? null : Map(assignment, includeAnswerKey);
    }

    internal static AssignmentDto Map(Assignment assignment, bool includeAnswerKey) =>
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

public sealed class GetAssignmentsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetAssignmentsQuery, IReadOnlyList<AssignmentDto>>
{
    public async Task<IReadOnlyList<AssignmentDto>> Handle(GetAssignmentsQuery query, CancellationToken cancellationToken)
    {
        var assignments = await dbContext.Assignments
            .AsNoTracking()
            .Include(x => x.Classroom!)
                .ThenInclude(c => c.Students)
            .Include(x => x.CreatedBy)
            .Include(x => x.Questions)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (query.ClassroomId is Guid classroomId)
        {
            assignments = assignments.Where(x => x.ClassroomId == classroomId).ToList();
        }

        var isTeacher = string.Equals(query.ViewerRole, nameof(UserRole.Teacher), StringComparison.OrdinalIgnoreCase);
        var isStudent = string.Equals(query.ViewerRole, nameof(UserRole.Student), StringComparison.OrdinalIgnoreCase);
        var isAdmin = string.Equals(query.ViewerRole, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase);

        if (isTeacher)
        {
            assignments = assignments.Where(x => x.Classroom?.TeacherId == query.ViewerUserId).ToList();
        }
        else if (isStudent)
        {
            assignments = assignments
                .Where(x => x.Classroom?.Students.Any(s => s.StudentId == query.ViewerUserId) == true)
                .ToList();
        }
        else if (!isAdmin)
        {
            assignments = [];
        }

        return assignments.Select(a => CreateAssignmentCommandHandler.Map(a, includeAnswerKey: isTeacher || isAdmin)).ToList();
    }
}

public sealed class GetAssignmentByIdQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetAssignmentByIdQuery, AssignmentDto?>
{
    public async Task<AssignmentDto?> Handle(GetAssignmentByIdQuery query, CancellationToken cancellationToken)
    {
        var includeKey = string.Equals(query.ViewerRole, nameof(UserRole.Teacher), StringComparison.OrdinalIgnoreCase)
            || string.Equals(query.ViewerRole, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase);
        return await CreateAssignmentCommandHandler.LoadAssignment(dbContext, query.AssignmentId, includeKey, cancellationToken);
    }
}

public sealed class SubmitAssignmentCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<SubmitAssignmentCommand, AssignmentSubmissionDto>
{
    public async Task<AssignmentSubmissionDto> Handle(SubmitAssignmentCommand command, CancellationToken cancellationToken)
    {
        var assignment = await dbContext.Assignments
            .Include(x => x.Questions)
            .Include(x => x.Classroom!)
                .ThenInclude(c => c.Students)
            .FirstOrDefaultAsync(x => x.Id == command.AssignmentId, cancellationToken)
            ?? throw new InvalidOperationException("Assignment not found.");

        if (assignment.Classroom?.Students.All(s => s.StudentId != command.StudentId) == true)
        {
            throw new InvalidOperationException("Student is not in this classroom.");
        }

        if (await dbContext.AssignmentSubmissions.AnyAsync(
                x => x.AssignmentId == assignment.Id && x.StudentId == command.StudentId, cancellationToken))
        {
            throw new InvalidOperationException("Assignment already submitted.");
        }

        var student = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.StudentId, cancellationToken)
            ?? throw new InvalidOperationException("Student not found.");

        var submission = new AssignmentSubmission
        {
            Id = Guid.NewGuid(),
            AssignmentId = assignment.Id,
            StudentId = student.Id,
            Status = AssignmentSubmissionStatus.Submitted,
            SubmittedAtUtc = DateTimeOffset.UtcNow
        };

        var autoScore = 0;
        var maxScore = assignment.Questions.Sum(x => x.Points);
        var allAutoGradable = true;

        foreach (var question in assignment.Questions)
        {
            var input = command.Answers.FirstOrDefault(x => x.QuestionId == question.Id);
            var answerText = (input?.AnswerText ?? string.Empty).Trim();
            bool? isCorrect = null;
            int? points = null;

            if (question.QuestionType == AssignmentQuestionType.MultipleChoice)
            {
                isCorrect = string.Equals(answerText, question.CorrectAnswer, StringComparison.OrdinalIgnoreCase);
                points = isCorrect == true ? question.Points : 0;
                if (isCorrect == true) autoScore += question.Points;
            }
            else
            {
                allAutoGradable = false;
                if (!string.IsNullOrWhiteSpace(question.CorrectAnswer) &&
                    string.Equals(answerText, question.CorrectAnswer, StringComparison.OrdinalIgnoreCase))
                {
                    isCorrect = true;
                    points = question.Points;
                    autoScore += question.Points;
                }
                else
                {
                    allAutoGradable = false;
                }
            }

            submission.Answers.Add(new AssignmentAnswer
            {
                Id = Guid.NewGuid(),
                SubmissionId = submission.Id,
                QuestionId = question.Id,
                AnswerText = answerText,
                IsCorrect = isCorrect,
                PointsAwarded = points
            });
        }

        submission.MaxScore = maxScore;
        if (allAutoGradable)
        {
            submission.Score = autoScore;
            submission.Status = AssignmentSubmissionStatus.Graded;
            submission.GradedAtUtc = DateTimeOffset.UtcNow;
            if (autoScore >= Math.Ceiling(maxScore * 0.7))
            {
                student.TotalXp += assignment.XpReward;
            }
        }

        dbContext.AssignmentSubmissions.Add(submission);
        await dbContext.SaveChangesAsync(cancellationToken);
        await BadgeAwarder.AwardEligibleAsync(dbContext, student, cancellationToken);

        return (await LoadSubmission(dbContext, submission.Id, cancellationToken))!;
    }

    internal static async Task<AssignmentSubmissionDto?> LoadSubmission(
        IAppDbContext dbContext,
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        var submission = await dbContext.AssignmentSubmissions
            .AsNoTracking()
            .Include(x => x.Student)
            .Include(x => x.Assignment)
            .Include(x => x.Answers)
                .ThenInclude(a => a.Question)
            .FirstOrDefaultAsync(x => x.Id == submissionId, cancellationToken);

        return submission is null ? null : MapSubmission(submission);
    }

    internal static AssignmentSubmissionDto MapSubmission(AssignmentSubmission submission) =>
        new(
            submission.Id,
            submission.AssignmentId,
            submission.Assignment?.Title ?? "Assignment",
            submission.StudentId,
            submission.Student?.DisplayName ?? "Student",
            submission.Status.ToString(),
            submission.Score,
            submission.MaxScore,
            submission.TeacherFeedback,
            submission.SubmittedAtUtc,
            submission.GradedAtUtc,
            submission.Answers.Select(a => new AssignmentAnswerReviewDto(
                a.QuestionId,
                a.Question?.Prompt ?? "",
                a.AnswerText,
                a.Question?.CorrectAnswer,
                a.IsCorrect,
                a.PointsAwarded,
                a.Question?.Points ?? 0)).ToList());
}

public sealed class GetAssignmentSubmissionsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetAssignmentSubmissionsQuery, IReadOnlyList<AssignmentSubmissionDto>>
{
    public async Task<IReadOnlyList<AssignmentSubmissionDto>> Handle(
        GetAssignmentSubmissionsQuery query,
        CancellationToken cancellationToken)
    {
        var assignment = await dbContext.Assignments
            .Include(x => x.Classroom)
            .FirstOrDefaultAsync(x => x.Id == query.AssignmentId, cancellationToken)
            ?? throw new InvalidOperationException("Assignment not found.");

        if (assignment.Classroom?.TeacherId != query.TeacherUserId)
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

public sealed class GradeSubmissionCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<GradeSubmissionCommand, AssignmentSubmissionDto>
{
    public async Task<AssignmentSubmissionDto> Handle(GradeSubmissionCommand command, CancellationToken cancellationToken)
    {
        var submission = await dbContext.AssignmentSubmissions
            .Include(x => x.Answers)
            .Include(x => x.Assignment!)
                .ThenInclude(a => a.Classroom)
            .Include(x => x.Assignment!)
                .ThenInclude(a => a.Questions)
            .FirstOrDefaultAsync(x => x.Id == command.SubmissionId, cancellationToken)
            ?? throw new InvalidOperationException("Submission not found.");

        if (submission.Assignment?.Classroom?.TeacherId != command.TeacherUserId)
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
