using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Application.Features.Assessments;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Application.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Quizzes;

public sealed class GetQuizByIdQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetQuizByIdQuery, QuizDto?>
{
    public async Task<QuizDto?> Handle(GetQuizByIdQuery query, CancellationToken cancellationToken)
    {
        var quiz = await dbContext.Quizzes
            .AsNoTracking()
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == query.QuizId, cancellationToken);

        if (quiz is null)
        {
            return null;
        }

        if (!PublishedAssessmentAccess.CanViewUnpublished(query.ViewerRole) && !quiz.IsPublished)
        {
            return null;
        }

        return GetQuizzesQueryHandler.Map(quiz);
    }
}
