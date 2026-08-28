using CodeKids.Domain.Entities;

namespace CodeKids.Application.Features.StudentAsk;

public static class StudentAskAccess
{
    public static bool IsEnabled(Course? course, bool unitEnabled = false, bool lessonEnabled = false) =>
        lessonEnabled || unitEnabled || (course?.StudentAskEnabled ?? false);
}
