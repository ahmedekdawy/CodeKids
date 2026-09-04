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
        var schoolType = ParseSchoolType(command.SchoolType);
        var audiences = await ResolveAudiencesAsync(dbContext, command.Grades, command.StageId, cancellationToken);
        var theme = string.IsNullOrWhiteSpace(command.Theme) ? "General" : command.Theme.Trim();
        var description = (command.Description ?? string.Empty).Trim();
        var ageMin = command.AgeMin is null or <= 0 ? 8 : command.AgeMin.Value;
        var ageMax = command.AgeMax is null or <= 0 ? 12 : command.AgeMax.Value;
        var sortOrder = command.SortOrder ?? 0;

        var courses = audiences.Select(audience => new Course
        {
            Id = Guid.NewGuid(),
            Title = title,
            Theme = theme,
            Description = description,
            AgeMin = ageMin,
            AgeMax = ageMax,
            TermId = term,
            Grade = audience.Grade,
            StageId = audience.StageId,
            SchoolType = schoolType,
            SortOrder = sortOrder,
            IsPublished = command.IsPublished
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

        if (!Enum.TryParse<CourseTerm>(value.Trim(), true, out var term) || !Enum.IsDefined(term))
        {
            throw new InvalidOperationException("Term must be FirstTerm, SecondTerm, or FullYear.");
        }

        return term;
    }

    internal static SchoolType ParseSchoolType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return SchoolType.All;
        }

        if (!Enum.TryParse<SchoolType>(value.Trim(), true, out var parsed)
            || parsed is not (SchoolType.Arabic or SchoolType.Language or SchoolType.All))
        {
            throw new InvalidOperationException("Course school type must be Arabic, Language, or All.");
        }

        return parsed;
    }

    /// <summary>
    /// Empty grades + no stage → one course for all grades.
    /// Empty grades + stage → one course covering every grade in that stage.
    /// Otherwise one course per distinct grade.
    /// </summary>
    internal static async Task<IReadOnlyList<(int? Grade, int? StageId)>> ResolveAudiencesAsync(
        IAppDbContext dbContext,
        IReadOnlyList<int>? grades,
        int? stageId,
        CancellationToken cancellationToken)
    {
        if (grades is null || grades.Count == 0)
        {
            if (stageId is null)
            {
                return [(null, null)];
            }

            var stage = await dbContext.Stages.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == stageId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Stage was not found.");
            return [(null, stage.Id)];
        }

        var requested = grades.Distinct().ToList();
        var matched = await dbContext.Grades.AsNoTracking()
            .Where(x => requested.Contains(x.Id))
            .Select(x => new { x.Id, x.StageId })
            .ToListAsync(cancellationToken);
        if (matched.Count != requested.Count)
        {
            throw new InvalidOperationException("Grade must be KG1, KG2, or between 1 and 12.");
        }

        if (stageId is not null && matched.Any(g => g.StageId != stageId.Value))
        {
            throw new InvalidOperationException("Selected grades must belong to the chosen stage.");
        }

        return matched
            .OrderBy(g => g.Id)
            .Select(g => ((int?)g.Id, (int?)g.StageId))
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

    internal static async Task<(int? Grade, int? StageId)> ResolveAudienceAsync(
        IAppDbContext dbContext,
        int? grade,
        int? stageId,
        CancellationToken cancellationToken)
    {
        var audiences = await ResolveAudiencesAsync(
            dbContext,
            grade is null ? [] : [grade.Value],
            stageId,
            cancellationToken);
        return audiences[0];
    }

    internal static CourseSummaryDto ToSummary(Course course) =>
        new(
            course.Id,
            course.Title,
            course.Theme,
            course.Description,
            course.AgeMin,
            course.AgeMax,
            course.TermId?.ToString(),
            course.Grade,
            course.StageId,
            course.SortOrder,
            course.SchoolType?.ToString() ?? nameof(SchoolType.All),
            course.ExternalSubjectId,
            course.SubjectCode,
            course.Category,
            course.TrackCode,
            course.TrackName,
            course.VerificationStatus,
            course.IsPublished);
}
