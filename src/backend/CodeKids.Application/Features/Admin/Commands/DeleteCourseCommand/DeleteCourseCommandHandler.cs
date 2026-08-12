using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Auth;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Admin;

public sealed class DeleteCourseCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteCourseCommand, bool>
{
    public async Task<bool> Handle(DeleteCourseCommand command, CancellationToken cancellationToken)
    {
        var course = await dbContext.Courses.FirstOrDefaultAsync(x => x.Id == command.CourseId, cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");

        var classrooms = await dbContext.Classrooms.Where(x => x.CourseId == course.Id).ToListAsync(cancellationToken);
        foreach (var classroom in classrooms)
        {
            classroom.CourseId = null;
        }

        var courseLinks = await dbContext.ClassroomCourses
            .Where(x => x.CourseId == course.Id)
            .ToListAsync(cancellationToken);
        dbContext.ClassroomCourses.RemoveRange(courseLinks);

        dbContext.Courses.Remove(course);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
