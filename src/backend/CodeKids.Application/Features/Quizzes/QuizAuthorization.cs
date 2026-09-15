using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;

namespace CodeKids.Application.Features.Quizzes;

internal static class QuizAuthorization
{
    internal static void EnsureCanManage(Quiz quiz, Guid teacherUserId, UserRole? actorRole = null)
    {
        if (actorRole == UserRole.SuperAdmin)
        {
            return;
        }

        var isCreator = quiz.CreatedByUserId == teacherUserId;
        if (!isCreator)
        {
            throw new InvalidOperationException("Only the quiz teacher can manage this quiz.");
        }
    }
}
