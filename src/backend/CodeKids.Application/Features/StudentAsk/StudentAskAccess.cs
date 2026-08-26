using CodeKids.Domain.Entities;

namespace CodeKids.Application.Features.StudentAsk;

public static class StudentAskAccess
{
    public static bool IsEnabled(Course? course, CourseUnit? unit, Lesson? lesson) =>
        (lesson?.StudentAskEnabled ?? false)
        || (unit?.StudentAskEnabled ?? false)
        || (course?.StudentAskEnabled ?? false);

    public static bool IsEnabled(Course? course, CourseUnit? unit) =>
        (unit?.StudentAskEnabled ?? false)
        || (course?.StudentAskEnabled ?? false);

    public static bool IsEnabled(Course? course) =>
        course?.StudentAskEnabled ?? false;
}
