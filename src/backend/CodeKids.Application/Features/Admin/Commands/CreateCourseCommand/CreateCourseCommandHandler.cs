using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Auth;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Admin;

public sealed class CreateCourseCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateCourseCommand, IReadOnlyList<CourseSummaryDto>>
{
    public async Task<IReadOnlyList<CourseSummaryDto>> Handle(CreateCourseCommand command, CancellationToken cancellationToken)
    {
        var title = command.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Course title is required.");
        }

        var term = ParseTerm(command.Term);
        var grades = NormalizeGrades(command.Grades);
        var theme = string.IsNullOrWhiteSpace(command.Theme) ? "General" : command.Theme.Trim();
        var description = (command.Description ?? string.Empty).Trim();
        var ageMin = command.AgeMin is null or <= 0 ? 8 : command.AgeMin.Value;
        var ageMax = command.AgeMax is null or <= 0 ? 12 : command.AgeMax.Value;
        var sortOrder = command.SortOrder ?? 0;

        var courses = grades.Select(grade => new Course
        {
            Id = Guid.NewGuid(),
            Title = title,
            Theme = theme,
            Description = description,
            AgeMin = ageMin,
            AgeMax = ageMax,
            Term = term,
            Grade = grade,
            SortOrder = sortOrder
        }).ToList();

        dbContext.Courses.AddRange(courses);
        await dbContext.SaveChangesAsync(cancellationToken);

        return courses.Select(ToSummary).ToList();
    }

    internal static CourseTerm? ParseTerm(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Enum.TryParse<CourseTerm>(value.Trim(), true, out var term))
        {
            throw new InvalidOperationException("Term must be FirstTerm, SecondTerm, or FullYear.");
        }

        return term;
    }

    /// <summary>
    /// Empty/null grades → one course for all grades (null).
    /// Otherwise one course per distinct grade (KG1=-1, KG2=0, or 1–12).
    /// </summary>
    internal static IReadOnlyList<int?> NormalizeGrades(IReadOnlyList<int>? grades)
    {
        if (grades is null || grades.Count == 0)
        {
            return [null];
        }

        return grades
            .Select(g => NormalizeGrade(g))
            .Distinct()
            .OrderBy(g => g ?? 999)
            .ToList();
    }

    /// <summary>KG1 = -1, KG2 = 0, grades 1–12; null means all grades.</summary>
    internal static int? NormalizeGrade(int? grade)
    {
        if (grade is null)
        {
            return null;
        }

        if (grade is < -1 or > 12)
        {
            throw new InvalidOperationException("Grade must be KG1, KG2, or between 1 and 12.");
        }

        return grade;
    }

    internal static CourseSummaryDto ToSummary(Course course) =>
        new(
            course.Id,
            course.Title,
            course.Theme,
            course.Description,
            course.AgeMin,
            course.AgeMax,
            course.Term?.ToString(),
            course.Grade,
            course.SortOrder);
}
