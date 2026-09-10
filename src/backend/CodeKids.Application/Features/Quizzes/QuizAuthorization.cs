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
        var isClassroomTeacher = quiz.Classroom?.Courses.Any(t => t.TeacherId == teacherUserId) == true;
        if (!isCreator && !isClassroomTeacher)
        {
            throw new InvalidOperationException("Only the quiz teacher can manage this quiz.");
        }
    }
}
