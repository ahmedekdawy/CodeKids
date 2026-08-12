using CodeKids.Domain.Enums;

namespace CodeKids.Domain;

public static class GradeStageHelper
{
    public static readonly IReadOnlyList<int> AllStages = [0, 1, 2, 3];

    /// <summary>Maps grade code (KG1=-1, KG2=0, 1–12) to stage 0–3.</summary>
    public static GradeStage? StageForGrade(int? grade)
    {
        if (grade is null)
        {
            return null;
        }

        return grade switch
        {
            -1 or 0 => GradeStage.Kg,
            >= 1 and <= 6 => GradeStage.Primary,
            >= 7 and <= 9 => GradeStage.Middle,
            >= 10 and <= 12 => GradeStage.Secondary,
            _ => null
        };
    }

    public static int? StageCodeForGrade(int? grade) =>
        StageForGrade(grade) is { } stage ? (int)stage : null;

    public static bool GradeMatchesStage(int? grade, int stage)
    {
        var code = StageCodeForGrade(grade);
        return code is not null && code.Value == stage;
    }

    /// <summary>
    /// Parses comma-separated stage codes. Empty/whitespace means all stages (legacy teachers).
    /// </summary>
    public static IReadOnlyList<int> ParseStages(string? stages)
    {
        if (string.IsNullOrWhiteSpace(stages))
        {
            return AllStages;
        }

        var parsed = stages
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var n) ? n : (int?)null)
            .Where(n => n is >= 0 and <= 3)
            .Select(n => n!.Value)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        return parsed.Count == 0 ? AllStages : parsed;
    }

    public static string SerializeStages(IReadOnlyList<int>? stages)
    {
        if (stages is null || stages.Count == 0)
        {
            return string.Join(',', AllStages);
        }

        var normalized = stages
            .Where(s => s is >= 0 and <= 3)
            .Distinct()
            .OrderBy(s => s)
            .ToList();

        if (normalized.Count == 0)
        {
            throw new InvalidOperationException("Teacher stages must be between 0 and 3.");
        }

        return string.Join(',', normalized);
    }

    public static bool TeacherCoversStage(string? teacherStages, int? grade)
    {
        var stage = StageCodeForGrade(grade);
        if (stage is null)
        {
            return true;
        }

        return ParseStages(teacherStages).Contains(stage.Value);
    }

    public static bool CourseMatchesClassroomGrade(int? courseGrade, int? classroomGrade)
    {
        if (classroomGrade is null)
        {
            return true;
        }

        // Null course grade = all grades; otherwise exact match.
        return courseGrade is null || courseGrade == classroomGrade;
    }
}
