using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using CodeKids.Application.Features.Assessments;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Application.Features.QuestionImages;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Classrooms;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Quizzes;

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

        var isTeacher = string.Equals(query.ViewerRole, nameof(UserRole.Teacher), StringComparison.OrdinalIgnoreCase);
        var isAdmin = string.Equals(query.ViewerRole, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase);
        if (isTeacher && query.ViewerUserId is Guid teacherId)
        {
            quizzesQuery = quizzesQuery.Where(x => x.CreatedByUserId == teacherId);
        }

        if (!PublishedAssessmentAccess.CanViewUnpublished(query.ViewerRole))
        {
            quizzesQuery = quizzesQuery.Where(x => x.IsPublished);
        }

        var quizzes = await quizzesQuery.ToListAsync(cancellationToken);
        if (StudentCompletedAssessments.IsStudent(query.ViewerRole) && query.ViewerUserId is Guid studentId)
        {
            var courseIds = await StudentCourseVisibility.GetAssessmentCourseIdsAsync(
                dbContext, studentId, cancellationToken);
            var submittedIds = await StudentCompletedAssessments.QuizIdsAsync(
                dbContext, studentId, cancellationToken);
            quizzes = quizzes
                .Where(x => courseIds.Contains(x.CourseId))
                .Where(x => !submittedIds.Contains(x.Id))
                .ToList();
        }

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
            quiz.DurationMinutes,
            quiz.IsPublished,
            QuizQuestionSync.MapTree(quiz.Questions));
}
