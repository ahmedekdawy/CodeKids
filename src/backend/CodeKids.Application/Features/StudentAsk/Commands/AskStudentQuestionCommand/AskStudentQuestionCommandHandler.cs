using System.Text;
using System.Text.Json;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Classrooms;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.StudentAsk;

public sealed class AskStudentQuestionCommandHandler(
    IAppDbContext dbContext,
    IStudyPlanAiClient aiClient)
    : ICommandHandler<AskStudentQuestionCommand, StudentAskAnswerDto>
{
    private const int MaxQuestionLength = 800;
    private const int MaxContextChars = 6000;

    private static readonly object AnswerSchema = new
    {
        type = "object",
        properties = new
        {
            inScope = new { type = "boolean" },
            answer = new { type = "string" }
        },
        required = new[] { "inScope", "answer" }
    };

    public async Task<StudentAskAnswerDto> Handle(
        AskStudentQuestionCommand command,
        CancellationToken cancellationToken)
    {
        var question = (command.Question ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new InvalidOperationException("Question is required.");
        }

        if (question.Length > MaxQuestionLength)
        {
            throw new InvalidOperationException("Question is too long.");
        }

        var (course, unit, lesson, context) = await ResolveScopeAsync(command, cancellationToken);

        var visible = await StudentCourseVisibility.GetVisibleCourseIdsAsync(
            dbContext, command.StudentId, cancellationToken);
        if (!visible.Contains(course.Id))
        {
            throw new InvalidOperationException("You do not have access to this course.");
        }

        var enabled = lesson is not null
            ? StudentAskAccess.IsEnabled(course, unit, lesson)
            : unit is not null
                ? StudentAskAccess.IsEnabled(course, unit)
                : StudentAskAccess.IsEnabled(course);

        if (!enabled)
        {
            throw new InvalidOperationException("Student Ask is not enabled for this course, unit, or lesson.");
        }

        string raw;
        try
        {
            raw = await aiClient.CompleteJsonAsync(
                BuildSystemPrompt(),
                BuildUserPrompt(course, unit, lesson, context, question),
                cancellationToken,
                AnswerSchema);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch
        {
            return OutOfScope();
        }

        return ParseAnswer(raw) ?? OutOfScope();
    }

    private async Task<(Course Course, CourseUnit? Unit, Lesson? Lesson, string Context)> ResolveScopeAsync(
        AskStudentQuestionCommand command,
        CancellationToken cancellationToken)
    {
        if (command.LessonId is Guid lessonId)
        {
            var lesson = await dbContext.Lessons
                    .AsNoTracking()
                    .Include(x => x.Course)
                    .Include(x => x.Unit)
                    .Include(x => x.Steps)
                    .FirstOrDefaultAsync(x => x.Id == lessonId, cancellationToken)
                ?? throw new InvalidOperationException("Lesson not found.");

            if (command.CourseId is Guid courseId && courseId != lesson.CourseId)
            {
                throw new InvalidOperationException("Lesson does not belong to the selected course.");
            }

            if (command.UnitId is Guid unitId && lesson.UnitId != unitId)
            {
                throw new InvalidOperationException("Lesson does not belong to the selected unit.");
            }

            var course = lesson.Course ?? throw new InvalidOperationException("Course not found.");
            return (course, lesson.Unit, lesson, BuildLessonContext(course, lesson.Unit, lesson));
        }

        if (command.UnitId is Guid onlyUnitId)
        {
            var unit = await dbContext.CourseUnits
                    .AsNoTracking()
                    .Include(x => x.Course)
                    .Include(x => x.Lessons)
                        .ThenInclude(x => x.Steps)
                    .FirstOrDefaultAsync(x => x.Id == onlyUnitId, cancellationToken)
                ?? throw new InvalidOperationException("Unit not found.");

            if (command.CourseId is Guid courseId && courseId != unit.CourseId)
            {
                throw new InvalidOperationException("Unit does not belong to the selected course.");
            }

            var course = unit.Course ?? throw new InvalidOperationException("Course not found.");
            return (course, unit, null, BuildUnitContext(course, unit));
        }

        if (command.CourseId is Guid onlyCourseId)
        {
            var course = await dbContext.Courses
                    .AsNoTracking()
                    .Include(x => x.Units)
                        .ThenInclude(x => x.Lessons)
                            .ThenInclude(x => x.Steps)
                    .Include(x => x.Lessons)
                        .ThenInclude(x => x.Steps)
                    .FirstOrDefaultAsync(x => x.Id == onlyCourseId, cancellationToken)
                ?? throw new InvalidOperationException("Course not found.");

            return (course, null, null, BuildCourseContext(course));
        }

        throw new InvalidOperationException("Select a course, unit, or lesson to ask about.");
    }

    private static string BuildSystemPrompt() =>
        """
        You are a school tutor for enrolled students. Answer only using the provided course, unit, and lesson material.
        If the question is off-topic, about other subjects, general trivia, homework from outside this content, or anything not covered by the material, set inScope to false.
        Never answer external or unrelated questions. Do not invent curriculum that is not in the material.
        Respond in the same language as the student question.
        Return JSON only: {"inScope": true|false, "answer": "..."}.
        When inScope is false, answer must briefly refuse and tell the student to ask about this lesson or unit only.
        """;

    private static string BuildUserPrompt(
        Course course,
        CourseUnit? unit,
        Lesson? lesson,
        string context,
        string question)
    {
        var scope = lesson is not null
            ? $"Lesson: {lesson.Title}"
            : unit is not null
                ? $"Unit: {unit.Title}"
                : $"Course: {course.Title}";

        return $"""
            Course: {course.Title}
            Scope: {scope}

            Allowed material:
            {context}

            Student question:
            {question}
            """;
    }

    private static string BuildLessonContext(Course course, CourseUnit? unit, Lesson lesson)
    {
        var sb = new StringBuilder();
        AppendCourseHeader(sb, course);
        if (unit is not null)
        {
            sb.AppendLine($"Unit: {unit.Title}");
            if (!string.IsNullOrWhiteSpace(unit.Description))
            {
                sb.AppendLine(unit.Description);
            }
        }

        AppendLesson(sb, lesson);
        return Truncate(sb.ToString());
    }

    private static string BuildUnitContext(Course course, CourseUnit unit)
    {
        var sb = new StringBuilder();
        AppendCourseHeader(sb, course);
        sb.AppendLine($"Unit: {unit.Title}");
        if (!string.IsNullOrWhiteSpace(unit.Description))
        {
            sb.AppendLine(unit.Description);
        }

        foreach (var lesson in unit.Lessons.OrderBy(x => x.SortOrder).ThenBy(x => x.Title))
        {
            AppendLesson(sb, lesson);
        }

        return Truncate(sb.ToString());
    }

    private static string BuildCourseContext(Course course)
    {
        var sb = new StringBuilder();
        AppendCourseHeader(sb, course);
        foreach (var unit in course.Units.OrderBy(x => x.SortOrder).ThenBy(x => x.Title))
        {
            sb.AppendLine($"Unit: {unit.Title}");
            if (!string.IsNullOrWhiteSpace(unit.Description))
            {
                sb.AppendLine(unit.Description);
            }

            foreach (var lesson in unit.Lessons.OrderBy(x => x.SortOrder).ThenBy(x => x.Title))
            {
                AppendLesson(sb, lesson);
            }
        }

        foreach (var lesson in course.Lessons.Where(x => x.UnitId is null).OrderBy(x => x.SortOrder))
        {
            AppendLesson(sb, lesson);
        }

        return Truncate(sb.ToString());
    }

    private static void AppendCourseHeader(StringBuilder sb, Course course)
    {
        sb.AppendLine($"Course: {course.Title}");
        if (!string.IsNullOrWhiteSpace(course.Description))
        {
            sb.AppendLine(course.Description);
        }
    }

    private static void AppendLesson(StringBuilder sb, Lesson lesson)
    {
        sb.AppendLine($"Lesson: {lesson.Title}");
        if (!string.IsNullOrWhiteSpace(lesson.Description))
        {
            sb.AppendLine(lesson.Description);
        }

        foreach (var step in lesson.Steps.OrderBy(x => x.StepNumber))
        {
            sb.AppendLine($"Step {step.StepNumber}: {step.Title}. {step.Prompt}");
        }
    }

    private static string Truncate(string text) =>
        text.Length <= MaxContextChars ? text : text[..MaxContextChars];

    private static StudentAskAnswerDto OutOfScope() =>
        new(false, "I can only answer questions about this selected course, unit, or lesson.");

    private static StudentAskAnswerDto? ParseAnswer(string raw)
    {
        var text = (raw ?? string.Empty).Trim();
        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf("```", StringComparison.Ordinal);
            if (start >= 0 && end > start)
            {
                text = text[start..end].Trim();
            }
        }

        var from = text.IndexOf('{');
        var to = text.LastIndexOf('}');
        if (from >= 0 && to > from)
        {
            text = text[from..(to + 1)];
        }

        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            var inScope = root.TryGetProperty("inScope", out var flag) && flag.ValueKind == JsonValueKind.True;
            var answer = root.TryGetProperty("answer", out var answerEl) && answerEl.ValueKind == JsonValueKind.String
                ? answerEl.GetString()?.Trim() ?? string.Empty
                : string.Empty;
            if (string.IsNullOrWhiteSpace(answer))
            {
                return inScope ? null : OutOfScope();
            }

            return new StudentAskAnswerDto(inScope, answer);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
