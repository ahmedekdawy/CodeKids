using CodeKids.Application.Abstractions;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

public sealed class AddStudentToClassroomCommandHandler(
    IAppDbContext dbContext,
    IWhatsAppClient whatsAppClient) : ICommandHandler<AddStudentToClassroomCommand, EnrollStudentResultDto>
{
    public async Task<EnrollStudentResultDto> Handle(AddStudentToClassroomCommand command, CancellationToken cancellationToken)
    {
        var classroom = await dbContext.Classrooms
            .Include(x => x.Courses)
                .ThenInclude(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == command.ClassroomId, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found.");

        var student = await dbContext.Users.FirstOrDefaultAsync(
            x => x.Id == command.StudentId && x.Role == UserRole.Student, cancellationToken)
            ?? throw new InvalidOperationException("Student not found.");

        var exists = await dbContext.ClassroomStudents.AnyAsync(
            x => x.ClassroomId == classroom.Id && x.StudentId == student.Id, cancellationToken);
        if (!exists)
        {
            dbContext.ClassroomStudents.Add(new ClassroomStudent
            {
                Id = Guid.NewGuid(),
                ClassroomId = classroom.Id,
                StudentId = student.Id,
                JoinedAtUtc = DateTimeOffset.UtcNow
            });
        }

        var requestedCourseIds = (command.CourseIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (requestedCourseIds.Count > 0)
        {
            var classroomCourseIds = classroom.Courses.Select(x => x.CourseId).ToHashSet();
            if (classroom.CourseId is Guid legacyCourseId)
            {
                classroomCourseIds.Add(legacyCourseId);
            }

            foreach (var courseId in requestedCourseIds)
            {
                if (!classroomCourseIds.Contains(courseId))
                {
                    throw new InvalidOperationException("One or more courses are not assigned to this classroom.");
                }
            }

            var courses = await dbContext.Courses
                .AsNoTracking()
                .Where(x => requestedCourseIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Grade })
                .ToListAsync(cancellationToken);

            if (courses.Count != requestedCourseIds.Count)
            {
                throw new InvalidOperationException("One or more courses were not found.");
            }

            if (courses.Any(c => c.Grade is not null && student.Grade is not null && c.Grade != student.Grade))
            {
                throw new InvalidOperationException("One or more courses do not match the student grade.");
            }

            var existingCourseIds = await dbContext.StudentCourseEnrollments
                .Where(x => x.ClassroomId == classroom.Id && x.StudentId == student.Id)
                .Select(x => x.CourseId)
                .ToListAsync(cancellationToken);
            var existingSet = existingCourseIds.ToHashSet();

            foreach (var courseId in requestedCourseIds)
            {
                if (existingSet.Contains(courseId))
                {
                    continue;
                }

                dbContext.StudentCourseEnrollments.Add(new StudentCourseEnrollment
                {
                    Id = Guid.NewGuid(),
                    StudentId = student.Id,
                    ClassroomId = classroom.Id,
                    CourseId = courseId,
                    EnrolledAtUtc = DateTimeOffset.UtcNow
                });
            }
        }

        var whatsAppStatus = "No student mobile on file.";
        var mobile = (student.MobilePhone ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(mobile))
        {
            classroom.WhatsAppNotifyPhones = MergePhones(classroom.WhatsAppNotifyPhones, mobile);

            var invite = classroom.WhatsAppGroupInviteUrl?.Trim() ?? string.Empty;
            var message = string.IsNullOrWhiteSpace(invite)
                ? $"CodeKids: You were enrolled in classroom \"{classroom.Name}\"."
                : $"CodeKids: You were enrolled in classroom \"{classroom.Name}\".\nJoin the WhatsApp group:\n{invite}";

            var send = await whatsAppClient.SendTextAsync(mobile, message, cancellationToken);
            whatsAppStatus = string.IsNullOrWhiteSpace(invite)
                ? $"Added {mobile} to classroom notify list. Invite link not set. WhatsApp: {send.Detail}"
                : $"Added {mobile} to classroom notify list and sent group invite. WhatsApp: {send.Detail}";
        }
        else if (!string.IsNullOrWhiteSpace(classroom.WhatsAppGroupInviteUrl))
        {
            whatsAppStatus = "Student enrolled, but has no mobile number — could not send WhatsApp group invite.";
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = (await CreateClassroomCommandHandler.LoadDto(dbContext, classroom.Id, cancellationToken))!;
        return new EnrollStudentResultDto(dto, whatsAppStatus);
    }

    internal static string MergePhones(string existing, string phone)
    {
        var phones = existing
            .Split([',', ';', ' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        if (!phones.Any(p => string.Equals(p, phone, StringComparison.OrdinalIgnoreCase)))
        {
            phones.Add(phone);
        }

        return string.Join(", ", phones);
    }
}
