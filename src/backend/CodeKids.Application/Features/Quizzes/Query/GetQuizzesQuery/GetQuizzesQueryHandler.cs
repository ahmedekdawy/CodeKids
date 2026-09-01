using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using CodeKids.Application.Features.Assessments;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Application.Features.QuestionImages;
using CodeKids.Application.Abstractions;
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
        if (!PublishedAssessmentAccess.CanViewUnpublished(query.ViewerRole))
        {
            quizzesQuery = quizzesQuery.Where(x => x.IsPublished);
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
            quiz.IsPublished,
            quiz.Questions
                .OrderBy(x => x.SortOrder)
                .Select(x => new QuizQuestionDto(
                    x.Id,
                    x.Prompt,
                    x.OptionA,
                    x.OptionB,
                    x.OptionC,
                    ChoiceOptions.Parse(x.OptionsJson, x.OptionA, x.OptionB, x.OptionC),
                    x.SortOrder,
                    QuestionImageUrls.Build(x.PromptImageMediaAssetId)))
                .ToList());
}
