namespace CodeKids.Application.Features.Assessments;

/// <summary>Shared rules for the optional time limit teachers put on quizzes and exams.</summary>
public static class AssessmentDuration
{
    public const int MinMinutes = 1;
    public const int MaxMinutes = 600;

    /// <summary>Minutes left when the student gets the "time is running out" warning.</summary>
    public const int WarnMinutes = 10;

    /// <summary>Turns teacher input into a stored value; anything at or below zero means untimed.</summary>
    public static int? Normalize(int? minutes) =>
        minutes is null or <= 0 ? null : Math.Clamp(minutes.Value, MinMinutes, MaxMinutes);
}
