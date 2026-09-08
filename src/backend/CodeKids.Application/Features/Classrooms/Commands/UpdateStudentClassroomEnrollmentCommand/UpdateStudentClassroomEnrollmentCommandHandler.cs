using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

public sealed class UpdateStudentClassroomEnrollmentCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<UpdateStudentClassroomEnrollmentCommand, ClassroomDto>
{
    public async Task<ClassroomDto> Handle(UpdateStudentClassroomEnrollmentCommand command, CancellationToken cancellationToken)
    {
        var classroom = await dbContext.Classrooms
            .Include(x => x.Courses)
            .FirstOrDefaultAsync(x => x.Id == command.ClassroomId, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found.");

        var student = await dbContext.Users.FirstOrDefaultAsync(
            x => x.Id == command.StudentId && x.Role == UserRole.Student, cancellationToken)
            ?? throw new InvalidOperationException("Student not found.");

        var enrolled = await dbContext.ClassroomStudents.AnyAsync(
            x => x.ClassroomId == classroom.Id && x.StudentId == student.Id, cancellationToken);
        if (!enrolled)
        {
            throw new InvalidOperationException("Student is not enrolled in this classroom.");
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
        }

        var existing = await dbContext.StudentCourseEnrollments
            .Where(x => x.ClassroomId == classroom.Id && x.StudentId == student.Id)
            .ToListAsync(cancellationToken);

        var requestedSet = requestedCourseIds.ToHashSet();
        var toRemove = existing.Where(x => !requestedSet.Contains(x.CourseId)).ToList();
        if (toRemove.Count > 0)
        {
            dbContext.StudentCourseEnrollments.RemoveRange(toRemove);
        }

        var existingSet = existing.Select(x => x.CourseId).ToHashSet();
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

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await CreateClassroomCommandHandler.LoadDto(dbContext, classroom.Id, cancellationToken))!;
    }
}
