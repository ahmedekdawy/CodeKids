using CodeKids.Application.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Assessments;

internal static class StudentCompletedAssessments
{
    internal static bool IsStudent(string? role) =>
        string.Equals(role, nameof(UserRole.Student), StringComparison.OrdinalIgnoreCase);

    internal static async Task<HashSet<Guid>> AssignmentIdsAsync(
        IAppDbContext dbContext,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var ids = await dbContext.AssignmentSubmissions
            .AsNoTracking()
            .Where(x => x.StudentId == studentId)
            .Select(x => x.AssignmentId)
            .ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }

    internal static async Task<HashSet<Guid>> ExamIdsAsync(
        IAppDbContext dbContext,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var ids = await dbContext.ExamAttempts
            .AsNoTracking()
            .Where(x => x.StudentId == studentId && x.Status != ExamAttemptStatus.InProgress)
            .Select(x => x.ExamId)
            .ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }

    internal static async Task<HashSet<Guid>> QuizIdsAsync(
        IAppDbContext dbContext,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var ids = await dbContext.QuizAttempts
            .AsNoTracking()
            .Where(x => x.UserId == studentId)
            .Select(x => x.QuizId)
            .ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }

    internal static Task<bool> HasAssignmentAsync(
        IAppDbContext dbContext,
        Guid studentId,
        Guid assignmentId,
        CancellationToken cancellationToken) =>
        dbContext.AssignmentSubmissions.AsNoTracking()
            .AnyAsync(x => x.StudentId == studentId && x.AssignmentId == assignmentId, cancellationToken);

    internal static Task<bool> HasExamAsync(
        IAppDbContext dbContext,
        Guid studentId,
        Guid examId,
        CancellationToken cancellationToken) =>
        dbContext.ExamAttempts.AsNoTracking()
            .AnyAsync(
                x => x.StudentId == studentId
                    && x.ExamId == examId
                    && x.Status != ExamAttemptStatus.InProgress,
                cancellationToken);

    internal static Task<bool> HasQuizAsync(
        IAppDbContext dbContext,
        Guid studentId,
        Guid quizId,
        CancellationToken cancellationToken) =>
        dbContext.QuizAttempts.AsNoTracking()
            .AnyAsync(x => x.UserId == studentId && x.QuizId == quizId, cancellationToken);
}
