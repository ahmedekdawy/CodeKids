using System.Text;
using System.Text.Json;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Classrooms;
using CodeKids.Application.Features.Courses;
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

        var enabled = StudentAskAccess.IsEnabled(
            course,
            unit?.StudentAskEnabled ?? false,
            lesson?.StudentAskEnabled ?? false);

        if (!enabled)
        {
            throw new InvalidOperationException("Student Ask is not enabled for this course, unit, or lesson.");
        }

        string raw;
        StudentAskAnswerDto dto;
        try
        {
            raw = await aiClient.CompleteJsonAsync(
                BuildSystemPrompt(),
                BuildUserPrompt(course, unit, lesson, context, question),
                cancellationToken,
                AnswerSchema);
            dto = ParseAnswer(raw) ?? OutOfScope();
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch
        {
            dto = OutOfScope();
        }

        await LogAskedQuestionAsync(command.StudentId, course, unit, lesson, question, dto, cancellationToken);
        return dto;
    }

    private async Task LogAskedQuestionAsync(
        Guid studentId,
        Course course,
        CourseUnitDto? unit,
        CourseLessonDto? lesson,
        string question,
        StudentAskAnswerDto dto,
        CancellationToken cancellationToken)
    {
        var studentName = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == studentId)
            .Select(x => x.DisplayName)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        dbContext.StudentAskedQuestions.Add(new StudentAskedQuestion
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            CourseId = course.Id,
            UnitId = unit?.Id,
            LessonId = lesson?.Id,
            CourseTitle = course.Title,
            UnitTitle = unit?.Title ?? string.Empty,
            LessonTitle = lesson?.Title ?? string.Empty,
            StudentName = studentName,
            Question = question,
            AiAnswer = dto.Answer,
            AiInScope = dto.InScope,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        try
        {

        
        await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {

            throw;
        }
    }

    private async Task<(Course Course, CourseUnitDto? Unit, CourseLessonDto? Lesson, string Context)> ResolveScopeAsync(
        AskStudentQuestionCommand command,
        CancellationToken cancellationToken)
    {
        if (command.LessonId is Guid lessonId)
        {
            var found = await CourseOutlineResolver.FindLessonAsync(dbContext, lessonId, cancellationToken)
                ?? throw new InvalidOperationException("Lesson not found.");
            var unitDto = CourseOutlineResolver.MapUnit(found.Course, found.Subject, found.Unit);
            var lessonDto = CourseOutlineResolver.MapLesson(found.Course, found.Subject, found.Unit, found.Lesson);
            if (command.CourseId is Guid courseId && courseId != found.Course.Id)
            {
                throw new InvalidOperationException("Lesson does not belong to the selected course.");
            }

            if (command.UnitId is Guid unitId && lessonDto.UnitId != unitId)
            {
                throw new InvalidOperationException("Lesson does not belong to the selected unit.");
            }

            return (found.Course, unitDto, lessonDto, await BuildLessonContextAsync(found.Course, unitDto, lessonDto, cancellationToken));
        }

        if (command.UnitId is Guid onlyUnitId)
        {
            var found = await CourseOutlineResolver.FindUnitAsync(dbContext, onlyUnitId, cancellationToken)
                ?? throw new InvalidOperationException("Unit not found.");
            var unitDto = CourseOutlineResolver.MapUnit(found.Course, found.Subject, found.Unit);
            if (command.CourseId is Guid courseId && courseId != found.Course.Id)
            {
                throw new InvalidOperationException("Unit does not belong to the selected course.");
            }

            return (found.Course, unitDto, null, BuildUnitContext(found.Course, unitDto));
        }

        if (command.CourseId is Guid onlyCourseId)
        {
            var course = await dbContext.Courses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == onlyCourseId, cancellationToken)
                ?? throw new InvalidOperationException("Course not found.");
            var outline = await CourseOutlineResolver.ResolveAsync(dbContext, course, cancellationToken);
            return (course, null, null, BuildCourseContext(course, outline));
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
        CourseUnitDto? unit,
        CourseLessonDto? lesson,
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

    private async Task<string> BuildLessonContextAsync(
        Course course,
        CourseUnitDto? unit,
        CourseLessonDto lesson,
        CancellationToken cancellationToken)
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

        await AppendLessonAsync(sb, lesson, cancellationToken);
        return Truncate(sb.ToString());
    }

    private static string BuildUnitContext(Course course, CourseUnitDto unit)
    {
        var sb = new StringBuilder();
        AppendCourseHeader(sb, course);
        sb.AppendLine($"Unit: {unit.Title}");
        if (!string.IsNullOrWhiteSpace(unit.Description))
        {
            sb.AppendLine(unit.Description);
        }

        foreach (var item in unit.Lessons.OrderBy(x => x.SortOrder).ThenBy(x => x.Title))
        {
            sb.AppendLine($"Lesson: {item.Title}");
            if (!string.IsNullOrWhiteSpace(item.Description))
            {
                sb.AppendLine(item.Description);
            }
        }

        return Truncate(sb.ToString());
    }

    private static string BuildCourseContext(Course course, CourseContentOutline outline)
    {
        var sb = new StringBuilder();
        AppendCourseHeader(sb, course);
        foreach (var unit in outline.Units.OrderBy(x => x.SortOrder).ThenBy(x => x.Title))
        {
            sb.AppendLine($"Unit: {unit.Title}");
            if (!string.IsNullOrWhiteSpace(unit.Description))
            {
                sb.AppendLine(unit.Description);
            }

            foreach (var lesson in unit.Lessons.OrderBy(x => x.SortOrder).ThenBy(x => x.Title))
            {
                sb.AppendLine($"Lesson: {lesson.Title}");
            }
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

    private async Task AppendLessonAsync(StringBuilder sb, CourseLessonDto lesson, CancellationToken cancellationToken)
    {
        sb.AppendLine($"Lesson: {lesson.Title}");
        if (!string.IsNullOrWhiteSpace(lesson.Description))
        {
            sb.AppendLine(lesson.Description);
        }

        var steps = await dbContext.LessonSteps
            .AsNoTracking()
            .Where(x => x.LessonId == lesson.Id)
            .OrderBy(x => x.StepNumber)
            .ToListAsync(cancellationToken);
        foreach (var step in steps)
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
