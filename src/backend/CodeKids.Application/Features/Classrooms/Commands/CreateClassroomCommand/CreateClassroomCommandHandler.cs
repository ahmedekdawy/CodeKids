using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Admin;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

public sealed class CreateClassroomCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateClassroomCommand, ClassroomDto>
{
    public async Task<ClassroomDto> Handle(CreateClassroomCommand command, CancellationToken cancellationToken)
    {
        var grade = CreateCourseCommandHandler.NormalizeGrade(command.Grade);
        var assignments = await ValidateCourseAssignments(dbContext, command.Courses, grade, cancellationToken);

        var classroom = new Classroom
        {
            Id = Guid.NewGuid(),
            Name = command.Name.Trim(),
            Description = (command.Description ?? string.Empty).Trim(),
            Grade = grade,
            CourseId = assignments.Count > 0 ? assignments[0].CourseId : null,
            WhatsAppGroupInviteUrl = (command.WhatsAppGroupInviteUrl ?? string.Empty).Trim(),
            ZoomLinksJson = ClassroomZoomLinks.Serialize(ClassroomZoomLinks.Normalize(command.ZoomLinks)),
            WhatsAppNotifyPhones = (command.WhatsAppNotifyPhones ?? string.Empty).Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        if (string.IsNullOrWhiteSpace(classroom.Name))
        {
            throw new InvalidOperationException("Classroom name is required.");
        }

        dbContext.Classrooms.Add(classroom);
        await ReplaceCourseAssignmentsAsync(dbContext, classroom.Id, assignments, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await LoadDto(dbContext, classroom.Id, cancellationToken))!;
    }

    internal static async Task<IReadOnlyList<ClassroomCourseAssignmentRequest>> ValidateCourseAssignments(
        IAppDbContext dbContext,
        IReadOnlyList<ClassroomCourseAssignmentRequest>? courses,
        int? classroomGrade,
        CancellationToken cancellationToken)
    {
        var assignments = (courses ?? [])
            .Where(x => x.CourseId != Guid.Empty && x.TeacherId != Guid.Empty)
            .GroupBy(x => x.CourseId)
            .Select(g => g.Last())
            .ToList();

        if (assignments.Count == 0) return assignments;

        var courseIds = assignments.Select(x => x.CourseId).ToList();
        var teacherIds = assignments.Select(x => x.TeacherId).Distinct().ToList();

        var matchedCourses = await dbContext.Courses
            .AsNoTracking()
            .Where(x => courseIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Grade, x.StageId })
            .ToListAsync(cancellationToken);
        if (matchedCourses.Count != courseIds.Count)
        {
            throw new InvalidOperationException("One or more courses were not found.");
        }

        if (classroomGrade is not null
            && matchedCourses.Any(c => !GradeStageHelper.CourseCoversGrade(c.Grade, c.StageId, classroomGrade)))
        {
            throw new InvalidOperationException("One or more courses do not match the classroom grade.");
        }

        var teachers = await dbContext.Users
            .AsNoTracking()
            .Where(x => teacherIds.Contains(x.Id) && x.Role == UserRole.Teacher)
            .Select(x => new { x.Id, x.Stages })
            .ToListAsync(cancellationToken);
        if (teachers.Count != teacherIds.Count)
        {
            throw new InvalidOperationException("One or more teachers were not found.");
        }

        if (classroomGrade is not null
            && teachers.Any(t => !GradeStageHelper.TeacherCoversStage(t.Stages, classroomGrade)))
        {
            throw new InvalidOperationException("One or more teachers are not assigned to this classroom stage.");
        }

        return assignments;
    }

    internal static async Task ReplaceCourseAssignmentsAsync(
        IAppDbContext dbContext,
        Guid classroomId,
        IReadOnlyList<ClassroomCourseAssignmentRequest> assignments,
        CancellationToken cancellationToken)
    {
        var existingCourses = await dbContext.ClassroomCourses
            .Where(x => x.ClassroomId == classroomId)
            .ToListAsync(cancellationToken);
        dbContext.ClassroomCourses.RemoveRange(existingCourses);

        var now = DateTimeOffset.UtcNow;
        foreach (var item in assignments)
        {
            dbContext.ClassroomCourses.Add(new ClassroomCourse
            {
                Id = Guid.NewGuid(),
                ClassroomId = classroomId,
                CourseId = item.CourseId,
                TeacherId = item.TeacherId,
                AssignedAtUtc = now
            });
        }

        var allowedCourseIds = assignments.Select(x => x.CourseId).ToHashSet();
        var staleEnrollments = await dbContext.StudentCourseEnrollments
            .Where(x => x.ClassroomId == classroomId && !allowedCourseIds.Contains(x.CourseId))
            .ToListAsync(cancellationToken);
        dbContext.StudentCourseEnrollments.RemoveRange(staleEnrollments);
    }

    internal static async Task<ClassroomDto?> LoadDto(IAppDbContext dbContext, Guid id, CancellationToken cancellationToken)
    {
        var classroom = await dbContext.Classrooms
            .AsNoTracking()
            .Include(x => x.Courses)
                .ThenInclude(x => x.Course)
            .Include(x => x.Courses)
                .ThenInclude(x => x.Teacher)
            .Include(x => x.Course)
            .Include(x => x.Students)
                .ThenInclude(x => x.Student)
            .Include(x => x.CourseEnrollments)
                .ThenInclude(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return classroom is null ? null : Map(classroom);
    }

    internal static bool HasTeacher(Classroom classroom, Guid teacherUserId) =>
        classroom.Courses.Any(x => x.TeacherId == teacherUserId);

    internal static ClassroomDto Map(Classroom classroom)
    {
        var courses = classroom.Courses
            .Where(x => x.Course is not null && x.Teacher is not null)
            .OrderBy(x => x.Course!.Grade ?? 999)
            .ThenBy(x => x.Course!.Title)
            .Select(x => new ClassroomCourseDto(
                x.CourseId,
                x.Course!.Title,
                x.Course.Grade,
                x.Course.StageId,
                x.Course.SchoolType?.ToString() ?? nameof(SchoolType.All),
                x.TeacherId,
                x.Teacher!.DisplayName))
            .ToList();

        var primary = courses.FirstOrDefault();
        var teachers = courses
            .GroupBy(x => x.TeacherId)
            .Select(g => new ClassroomTeacherDto(g.Key, g.First().TeacherName))
            .OrderBy(x => x.DisplayName)
            .ToList();

        return new(
            classroom.Id,
            classroom.Name,
            classroom.Description,
            classroom.Grade,
            teachers,
            courses,
            primary?.CourseId ?? classroom.CourseId,
            primary?.CourseTitle ?? classroom.Course?.Title,
            primary?.CourseGrade ?? classroom.Course?.Grade,
            primary?.CourseStageId ?? classroom.Course?.StageId,
            primary?.CourseSchoolType ?? classroom.Course?.SchoolType?.ToString() ?? nameof(SchoolType.All),
            classroom.WhatsAppGroupInviteUrl,
            ClassroomZoomLinks.Parse(classroom.ZoomLinksJson),
            classroom.WhatsAppNotifyPhones,
            classroom.DailyWhatsAppReportsEnabled,
            classroom.Students
                .Where(x => x.Student is not null)
                .Select(x =>
                {
                    var enrolled = (classroom.CourseEnrollments ?? [])
                        .Where(e => e.StudentId == x.StudentId)
                        .ToList();
                    return new ClassroomStudentDto(
                        x.StudentId,
                        x.Student!.DisplayName,
                        x.Student.Email,
                        x.Student.MobilePhone,
                        enrolled.Select(e => e.CourseId).ToList(),
                        enrolled
                            .Select(e => e.Course?.Title)
                            .Where(t => !string.IsNullOrWhiteSpace(t))
                            .Cast<string>()
                            .OrderBy(t => t)
                            .ToList());
                })
                .OrderBy(x => x.DisplayName)
                .ToList());
    }
}
