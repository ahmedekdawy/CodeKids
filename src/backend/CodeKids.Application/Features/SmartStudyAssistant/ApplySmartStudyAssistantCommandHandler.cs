using System.Text.Json;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Assignments;
using CodeKids.Application.Features.Courses;
using CodeKids.Application.Features.Exams;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Application.Features.Quizzes;
using CodeKids.Application.Features.StudyPlans;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.SmartStudyAssistant;

/// <summary>
/// Maps Smart Study Assistant output onto real course resources: units/lessons on
/// the course tree, questions into the bank, or a full quiz / assignment / exam.
/// </summary>
public sealed class ApplySmartStudyAssistantCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateQuizCommand, QuizDto> createQuiz,
    ICommandHandler<CreateAssignmentCommand, AssignmentDto> createAssignment,
    ICommandHandler<CreateBankQuestionCommand, BankQuestionDto> createBankQuestion)
    : ICommandHandler<ApplySmartStudyAssistantCommand, ApplySmartStudyAssistantResultDto>
{
    private const int MaxQuestions = 50;
    private const int MaxUnits = 30;

    public async Task<ApplySmartStudyAssistantResultDto> Handle(
        ApplySmartStudyAssistantCommand command,
        CancellationToken cancellationToken)
    {
        var course = await dbContext.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.CourseId, cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");

        await StudyPlanAccess.EnsureTeacherOwnsCourseAsync(
            dbContext, command.TeacherId, course.Id, cancellationToken);

        return (command.Target ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "tree" or "outline" => await ApplyToTreeAsync(command, cancellationToken),
            "bank" or "question-bank" => await ApplyToBankAsync(command, cancellationToken),
            "quiz" => await ApplyToQuizAsync(command, cancellationToken),
            "assignment" => await ApplyToAssignmentAsync(command, cancellationToken),
            "exam" => await ApplyToExamAsync(command, cancellationToken),
            _ => throw new InvalidOperationException(
                "Target must be one of: tree, bank, quiz, assignment, exam.")
        };
    }

    private async Task<ApplySmartStudyAssistantResultDto> ApplyToTreeAsync(
        ApplySmartStudyAssistantCommand command,
        CancellationToken cancellationToken)
    {
        var units = NormalizeUnits(command.Units);
        if (units.Count == 0)
        {
            throw new InvalidOperationException("No units were provided to map.");
        }

        var course = await dbContext.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.CourseId, cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");

        // Reuse the existing subject tree so new units merge into the right subject.
        var subjects = await CourseOutlineResolver.LoadRelatedSubjectsAsync(dbContext, course, cancellationToken);
        if (subjects.Count == 0)
        {
            throw new InvalidOperationException("No subject tree is linked to this course. Generate a course tree first.");
        }
        var subject = subjects[0];

        var existingUnits = await dbContext.SubjectUnits
            .Include(x => x.Lessons)
            .Where(x => x.SubjectId == subject.Id)
            .ToListAsync(cancellationToken);

        var unitsCreated = 0;
        var lessonsCreated = 0;
        foreach (var draftUnit in units)
        {
            var unit = existingUnits.FirstOrDefault(u => TitlesMatch(u.Title, draftUnit.Title));
            if (unit is null)
            {
                unit = new SubjectUnit
                {
                    SubjectId = subject.Id,
                    Title = draftUnit.Title,
                    SortOrder = draftUnit.SortOrder
                };
                dbContext.SubjectUnits.Add(unit);
                await dbContext.SaveChangesAsync(cancellationToken);
                existingUnits.Add(unit);
                unitsCreated++;
            }

            var nextLessonOrder = unit.Lessons.Count > 0 ? unit.Lessons.Max(l => l.SortOrder) + 1 : 1;
            foreach (var lessonTitle in draftUnit.Lessons)
            {
                if (unit.Lessons.Any(l => TitlesMatch(l.Title, lessonTitle)))
                {
                    continue;
                }

                dbContext.SubjectUnitLessons.Add(new SubjectUnitLesson
                {
                    SubjectUnitId = unit.Id,
                    Title = lessonTitle,
                    SortOrder = nextLessonOrder++
                });
                lessonsCreated++;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new ApplySmartStudyAssistantResultDto(
            "tree",
            null,
            null,
            0,
            unitsCreated,
            lessonsCreated,
            [],
            $"تم إضافة {unitsCreated} وحدة و{lessonsCreated} درس إلى فهرس المادة.");
    }

    private async Task<ApplySmartStudyAssistantResultDto> ApplyToBankAsync(
        ApplySmartStudyAssistantCommand command,
        CancellationToken cancellationToken)
    {
        var questions = NormalizeQuestions(command.Questions);
        if (questions.Count == 0)
        {
            throw new InvalidOperationException("No questions were provided to save.");
        }

        var questionIds = new List<Guid>();
        foreach (var question in questions)
        {
            var saved = await createBankQuestion.Handle(
                new CreateBankQuestionCommand(
                    command.TeacherId,
                    command.CourseId,
                    LessonId: command.LessonId,
                    question.QuestionType,
                    question.Prompt,
                    PassageText: null,
                    OptionA: null,
                    OptionB: null,
                    OptionC: null,
                    OptionD: null,
                    question.Options,
                    BankCorrectAnswer(question),
                    question.Points,
                    question.SortOrder,
                    PromptImageMediaAssetId: null,
                    Children: null),
                cancellationToken);
            questionIds.Add(saved.Id);
        }

        return new ApplySmartStudyAssistantResultDto(
            "bank",
            null,
            null,
            questionIds.Count,
            0,
            0,
            questionIds,
            $"تم حفظ {questionIds.Count} سؤال في بنك الأسئلة.");
    }

    private async Task<ApplySmartStudyAssistantResultDto> ApplyToQuizAsync(
        ApplySmartStudyAssistantCommand command,
        CancellationToken cancellationToken)
    {
        var questions = NormalizeQuestions(command.Questions, requireOptions: true);
        if (questions.Count == 0)
        {
            throw new InvalidOperationException("No valid multiple-choice questions were provided.");
        }

        var quizInputs = questions.Select(question => new CreateQuizQuestionInput(
            question.Prompt,
            OptionA: null,
            OptionB: null,
            OptionC: null,
            question.Options,
            question.CorrectOption,
            question.SortOrder,
            PromptImageMediaAssetId: null,
            Id: null,
            question.QuestionType,
            PassageText: null,
            CorrectAnswer: BankCorrectAnswer(question),
            question.Points)).ToList();

        var quiz = await createQuiz.Handle(
            new CreateQuizCommand(
                command.TeacherId,
                command.CourseId,
                ClassroomId: null,
                command.Title,
                command.Description,
                XpReward: 20,
                IsPublished: false,
                quizInputs),
            cancellationToken);

        return new ApplySmartStudyAssistantResultDto(
            "quiz",
            quiz.Id,
            $"/quizzes/{quiz.Id}",
            quizInputs.Count,
            0,
            0,
            [],
            $"تم إنشاء كويز «{quiz.Title}» بـ {quizInputs.Count} سؤال (غير منشور).");
    }

    private async Task<ApplySmartStudyAssistantResultDto> ApplyToAssignmentAsync(
        ApplySmartStudyAssistantCommand command,
        CancellationToken cancellationToken)
    {
        var questions = NormalizeQuestions(command.Questions);
        if (questions.Count == 0)
        {
            throw new InvalidOperationException("No valid questions were provided.");
        }

        var classroomId = await ResolveClassroomIdAsync(command, cancellationToken)
            ?? throw new InvalidOperationException(
                "No classroom is assigned to this course for this teacher. Assign the course to a classroom first.");

        var assignmentInputs = questions.Select(question => new AssignmentQuestionInput(
            question.Prompt,
            question.QuestionType,
            OptionA: null,
            OptionB: null,
            OptionC: null,
            BankCorrectAnswer(question),
            question.Points,
            question.SortOrder,
            PromptImageMediaAssetId: null)).ToList();

        var assignment = await createAssignment.Handle(
            new CreateAssignmentCommand(
                command.TeacherId,
                classroomId,
                command.CourseId,
                command.Title,
                command.Description,
                DueAtUtc: null,
                XpReward: 20,
                IsPublished: false,
                assignmentInputs),
            cancellationToken);

        return new ApplySmartStudyAssistantResultDto(
            "assignment",
            assignment.Id,
            $"/assignments/{assignment.Id}",
            assignmentInputs.Count,
            0,
            0,
            [],
            $"تم إنشاء واجب «{assignment.Title}» بـ {assignmentInputs.Count} سؤال (غير منشور).");
    }

    private async Task<ApplySmartStudyAssistantResultDto> ApplyToExamAsync(
        ApplySmartStudyAssistantCommand command,
        CancellationToken cancellationToken)
    {
        // Exams reference bank questions — save to bank first, then hand back IDs
        // so the teacher finishes the exam in the exams screen.
        var bankResult = await ApplyToBankAsync(command, cancellationToken);
        return new ApplySmartStudyAssistantResultDto(
            "exam",
            null,
            null,
            bankResult.QuestionsCreated,
            0,
            0,
            bankResult.QuestionIds,
            $"تم حفظ {bankResult.QuestionsCreated} سؤال في بنك الأسئلة — أكمل إنشاء الاختبار من شاشة الاختبارات باستخدام هذه الأسئلة.");
    }

    private async Task<Guid?> ResolveClassroomIdAsync(
        ApplySmartStudyAssistantCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UnitId is Guid unitId && unitId != Guid.Empty)
        {
            // UnitId is not a classroom here; callers pass classroomId in Description field never —
            // resolve the first classroom assigned to this teacher that has this course.
        }

        var classroomCourse = await dbContext.ClassroomCourses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TeacherId == command.TeacherId && x.CourseId == command.CourseId, cancellationToken);
        return classroomCourse?.ClassroomId;
    }


    private static List<ApplySmartStudyAssistantQuestionInput> NormalizeQuestions(
        IReadOnlyList<ApplySmartStudyAssistantQuestionInput>? raw,
        bool requireOptions = false)
    {
        var result = new List<ApplySmartStudyAssistantQuestionInput>();
        foreach (var question in raw ?? [])
        {
            if (string.IsNullOrWhiteSpace(question.Prompt))
            {
                continue;
            }

            var options = (question.Options ?? [])
                .Select(o => (o ?? string.Empty).Trim())
                .Where(o => o.Length > 0)
                .Take(4)
                .ToList();

            var type = (question.QuestionType ?? string.Empty).Trim();
            if (!Enum.TryParse<BankQuestionType>(type, true, out _))
            {
                type = options.Count >= 2 ? nameof(BankQuestionType.Choose) : nameof(BankQuestionType.ShortAnswer);
            }

            if (requireOptions && options.Count < 2)
            {
                continue;
            }

            var correct = ResolveCorrectKey(options, question.CorrectOption, question.CorrectAnswer)
                ?? (options.Count >= 2 ? "A" : question.CorrectAnswer ?? string.Empty);

            result.Add(new ApplySmartStudyAssistantQuestionInput(
                question.Prompt.Trim()[..Math.Min(question.Prompt.Trim().Length, 400)],
                type,
                options,
                correct,
                correct,
                question.Points > 0 ? question.Points : 1,
                result.Count + 1));

            if (result.Count >= MaxQuestions)
            {
                break;
            }
        }

        return result;
    }

    private static List<ApplySmartStudyAssistantUnitInput> NormalizeUnits(
        IReadOnlyList<ApplySmartStudyAssistantUnitInput>? raw)
    {
        var result = new List<ApplySmartStudyAssistantUnitInput>();
        foreach (var unit in raw ?? [])
        {
            if (string.IsNullOrWhiteSpace(unit.Title))
            {
                continue;
            }

            var lessons = (unit.Lessons ?? [])
                .Select(l => (l ?? string.Empty).Trim())
                .Where(l => l.Length > 0)
                .ToList();

            result.Add(new ApplySmartStudyAssistantUnitInput(
                unit.Title.Trim()[..Math.Min(unit.Title.Trim().Length, 200)],
                unit.SortOrder > 0 ? unit.SortOrder : result.Count + 1,
                lessons));

            if (result.Count >= MaxUnits)
            {
                break;
            }
        }

        return result;
    }

    private static string? ResolveCorrectKey(
        IReadOnlyList<string> options,
        string? correctOption,
        string? correctAnswer)
    {
        var raw = new[] { correctOption, correctAnswer }.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
        if (string.IsNullOrWhiteSpace(raw) || options.Count == 0)
        {
            return null;
        }

        var trimmed = raw.Trim();
        if (trimmed.Length == 1)
        {
            var index = char.ToUpperInvariant(trimmed[0]) - 'A';
            if (index >= 0 && index < options.Count)
            {
                return ((char)('A' + index)).ToString();
            }
        }

        for (var i = 0; i < options.Count; i++)
        {
            if (string.Equals(options[i], trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return ((char)('A' + i)).ToString();
            }
        }

        return null;
    }

    private static string BankCorrectAnswer(ApplySmartStudyAssistantQuestionInput question)
    {
        if (string.Equals(question.QuestionType, nameof(BankQuestionType.TrueFalse), StringComparison.OrdinalIgnoreCase))
        {
            var key = string.IsNullOrWhiteSpace(question.CorrectOption)
                ? question.CorrectAnswer
                : question.CorrectOption;
            return string.Equals(key?.Trim(), "B", StringComparison.OrdinalIgnoreCase) ? "False" : "True";
        }

        return string.IsNullOrWhiteSpace(question.CorrectOption)
            ? question.CorrectAnswer
            : question.CorrectOption;
    }

    private static bool TitlesMatch(string? left, string? right)
    {
        static string Normalize(string? value) =>
            System.Text.RegularExpressions.Regex.Replace(
                (value ?? string.Empty).Trim().ToLowerInvariant(), @"\s+", " ");

        var a = Normalize(left);
        var b = Normalize(right);
        return a.Length > 0 && a == b;
    }
}
