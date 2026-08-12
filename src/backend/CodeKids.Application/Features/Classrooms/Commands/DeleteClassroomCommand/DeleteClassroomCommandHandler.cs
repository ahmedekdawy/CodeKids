using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Admin;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

public sealed class DeleteClassroomCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteClassroomCommand, bool>
{
    public async Task<bool> Handle(DeleteClassroomCommand command, CancellationToken cancellationToken)
    {
        var classroom = await dbContext.Classrooms.FirstOrDefaultAsync(x => x.Id == command.ClassroomId, cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found.");

        var courseLinks = await dbContext.ClassroomCourses
            .Where(x => x.ClassroomId == classroom.Id)
            .ToListAsync(cancellationToken);
        dbContext.ClassroomCourses.RemoveRange(courseLinks);

        var memberships = await dbContext.ClassroomStudents
            .Where(x => x.ClassroomId == classroom.Id)
            .ToListAsync(cancellationToken);
        dbContext.ClassroomStudents.RemoveRange(memberships);

        var sessions = await dbContext.LiveSessions
            .Where(x => x.ClassroomId == classroom.Id)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.ClassroomId = null;
        }

        var quizzes = await dbContext.Quizzes.Where(x => x.ClassroomId == classroom.Id).ToListAsync(cancellationToken);
        foreach (var quiz in quizzes)
        {
            quiz.ClassroomId = null;
        }

        var assignments = await dbContext.Assignments.Where(x => x.ClassroomId == classroom.Id).ToListAsync(cancellationToken);
        dbContext.Assignments.RemoveRange(assignments);

        dbContext.Classrooms.Remove(classroom);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
