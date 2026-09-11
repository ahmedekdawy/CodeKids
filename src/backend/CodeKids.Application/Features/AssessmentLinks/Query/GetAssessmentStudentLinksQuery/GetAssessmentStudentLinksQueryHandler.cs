using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.AssessmentLinks;

public sealed class GetAssessmentStudentLinksQueryHandler(
    IAppDbContext dbContext,
    IAssessmentAccessTokenService tokenService)
    : IQueryHandler<GetAssessmentStudentLinksQuery, AssessmentStudentLinksResult>
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromDays(90);

    public async Task<AssessmentStudentLinksResult> Handle(
        GetAssessmentStudentLinksQuery query,
        CancellationToken cancellationToken)
    {
        EnsureTeacher(query.ViewerRole);

        Guid? classroomId;
        string title;
        DateTimeOffset? dueAtUtc;

        switch (query.Kind)
        {
            case AssessmentLinkKind.Exam:
            {
                var exam = await dbContext.Exams.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == query.ResourceId, cancellationToken)
                    ?? throw new InvalidOperationException("Exam not found.");
                classroomId = exam.ClassroomId;
                title = exam.Title;
                dueAtUtc = exam.DueAtUtc;
                break;
            }
            case AssessmentLinkKind.Quiz:
            {
                var quiz = await dbContext.Quizzes.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == query.ResourceId, cancellationToken)
                    ?? throw new InvalidOperationException("Quiz not found.");
                classroomId = quiz.ClassroomId;
                title = quiz.Title;
                dueAtUtc = null;
                break;
            }
            case AssessmentLinkKind.Assignment:
            {
                var assignment = await dbContext.Assignments.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == query.ResourceId, cancellationToken)
                    ?? throw new InvalidOperationException("Assignment not found.");
                classroomId = assignment.ClassroomId;
                title = assignment.Title;
                dueAtUtc = assignment.DueAtUtc;
                break;
            }
            default:
                throw new InvalidOperationException("Unsupported assessment kind.");
        }

        if (classroomId is null)
        {
            throw new InvalidOperationException("This assessment is not linked to a classroom.");
        }

        var students = await dbContext.ClassroomStudents.AsNoTracking()
            .Where(x => x.ClassroomId == classroomId.Value)
            .Include(x => x.Student)
            .OrderBy(x => x.Student!.DisplayName)
            .ToListAsync(cancellationToken);

        var lifetime = ResolveLifetime(dueAtUtc);
        var links = new List<AssessmentStudentLinkDto>(students.Count);

        foreach (var enrollment in students)
        {
            var student = enrollment.Student;
            if (student is null || student.Role != UserRole.Student || !student.IsActive)
            {
                continue;
            }

            var key = tokenService.CreateToken(query.Kind, query.ResourceId, student.Id, lifetime);
            links.Add(new AssessmentStudentLinkDto(
                student.Id,
                student.DisplayName,
                student.Email,
                string.IsNullOrWhiteSpace(student.MobilePhone) ? null : student.MobilePhone,
                key,
                $"/go/{key}"));
        }

        return new AssessmentStudentLinksResult(query.Kind, query.ResourceId, title, links);
    }

    private static TimeSpan ResolveLifetime(DateTimeOffset? dueAtUtc)
    {
        if (dueAtUtc is DateTimeOffset due && due > DateTimeOffset.UtcNow)
        {
            var untilDue = due - DateTimeOffset.UtcNow + TimeSpan.FromDays(7);
            return untilDue > DefaultLifetime ? untilDue : DefaultLifetime;
        }

        return DefaultLifetime;
    }

    private static void EnsureTeacher(string role)
    {
        if (string.Equals(role, nameof(UserRole.Teacher), StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new UnauthorizedAccessException("Only teachers can generate student assessment links.");
    }
}
