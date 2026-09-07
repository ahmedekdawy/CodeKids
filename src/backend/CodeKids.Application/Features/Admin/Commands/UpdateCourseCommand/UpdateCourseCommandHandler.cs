using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Auth;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Admin;

public sealed class UpdateCourseCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<UpdateCourseCommand, CourseSummaryDto>
{
    public async Task<CourseSummaryDto> Handle(UpdateCourseCommand command, CancellationToken cancellationToken)
    {
        var course = await dbContext.Courses.FirstOrDefaultAsync(x => x.Id == command.CourseId, cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");

        var title = command.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Course title is required.");
        }

        course.Title = title;
        course.Theme = string.IsNullOrWhiteSpace(command.Theme) ? "General" : command.Theme.Trim();
        course.Description = (command.Description ?? string.Empty).Trim();
        course.AgeMin = command.AgeMin is null or <= 0 ? 8 : command.AgeMin.Value;
        course.AgeMax = command.AgeMax is null or <= 0 ? 12 : command.AgeMax.Value;
        course.TermId = CreateCourseCommandHandler.ParseTerm(command.Term);
        var audience = await CreateCourseCommandHandler.ResolveAudienceAsync(
            dbContext, command.Grade, command.StageId, cancellationToken);
        course.Grade = audience.Grade;
        course.StageId = audience.StageId;
        course.SchoolType = CreateCourseCommandHandler.ParseSchoolType(command.SchoolType);
        course.SortOrder = command.SortOrder ?? 0;
        if (command.IsPublished is bool isPublished)
        {
            course.IsPublished = isPublished;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return CreateCourseCommandHandler.ToSummary(course);
    }
}
