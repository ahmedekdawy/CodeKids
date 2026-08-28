using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Courses;

public sealed class CreateCourseUnitCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateCourseUnitCommand, CourseUnitDto>
{
    public async Task<CourseUnitDto> Handle(CreateCourseUnitCommand command, CancellationToken cancellationToken)
    {
        var title = (command.Title ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Unit title is required.");
        }

        var course = await dbContext.Courses
                .FirstOrDefaultAsync(x => x.Id == command.CourseId, cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");
        var subjects = await CourseOutlineResolver.LoadRelatedSubjectsAsync(dbContext, course, cancellationToken);
        var subject = subjects.FirstOrDefault()
            ?? throw new InvalidOperationException("Course has no linked subject catalog.");

        var unit = new SubjectUnit
        {
            SubjectId = subject.Id,
            Title = CourseOutlineResolver.Clamp(title, 300),
            SortOrder = Math.Max(1, command.SortOrder)
        };
        dbContext.SubjectUnits.Add(unit);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CourseOutlineResolver.MapUnit(course, subject, unit);
    }
}
