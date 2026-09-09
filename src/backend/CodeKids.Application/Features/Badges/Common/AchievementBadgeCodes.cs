namespace CodeKids.Application.Features.Badges;

public static class AchievementBadgeCodes
{
    public const string WeeklyStar = "WEEKLY_STAR";
    public const string AssignmentAce = "ASSIGNMENT_ACE";
    public const string ExamStar = "EXAM_STAR";
    public const string QuizAce = "QUIZ_ACE";

    public static readonly HashSet<string> All = new(StringComparer.Ordinal)
    {
        WeeklyStar,
        AssignmentAce,
        ExamStar,
        QuizAce
    };
}
