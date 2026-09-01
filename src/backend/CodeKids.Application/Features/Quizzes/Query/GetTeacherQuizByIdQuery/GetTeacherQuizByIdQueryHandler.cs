using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Application.Features.QuestionImages;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Quizzes;

public sealed class GetTeacherQuizByIdQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetTeacherQuizByIdQuery, TeacherQuizDetailDto?>
{
    public async Task<TeacherQuizDetailDto?> Handle(
        GetTeacherQuizByIdQuery query,
        CancellationToken cancellationToken)
    {
        var quiz = await dbContext.Quizzes
            .AsNoTracking()
            .Include(x => x.Questions)
            .Include(x => x.Classroom)
                .ThenInclude(c => c!.Courses)
            .FirstOrDefaultAsync(x => x.Id == query.QuizId, cancellationToken);

        if (quiz is null)
        {
            return null;
        }

        QuizAuthorization.EnsureCanManage(quiz, query.TeacherUserId);

        return new TeacherQuizDetailDto(
            quiz.Id,
            quiz.CourseId,
            quiz.ClassroomId,
            quiz.Title,
            quiz.Description,
            quiz.XpReward,
            quiz.Questions
                .OrderBy(x => x.SortOrder)
                .Select(x => new TeacherQuizQuestionDetailDto(
                    x.Id,
                    x.Prompt,
                    ChoiceOptions.Parse(x.OptionsJson, x.OptionA, x.OptionB, x.OptionC),
                    x.CorrectOption,
                    x.SortOrder,
                    x.PromptImageMediaAssetId,
                    QuestionImageUrls.Build(x.PromptImageMediaAssetId)))
                .ToList());
    }
}
