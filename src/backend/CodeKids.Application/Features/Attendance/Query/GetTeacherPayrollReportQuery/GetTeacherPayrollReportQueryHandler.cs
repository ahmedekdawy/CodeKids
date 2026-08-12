using CodeKids.Application.Abstractions;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Attendance;

public sealed class GetTeacherPayrollReportQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetTeacherPayrollReportQuery, TeacherPayrollReportDto>
{
    public async Task<TeacherPayrollReportDto> Handle(
        GetTeacherPayrollReportQuery query,
        CancellationToken cancellationToken)
    {
        if (query.ToDate < query.FromDate)
        {
            throw new InvalidOperationException("End date must be on or after the start date.");
        }

        if (query.Stage is not null and not (>= 0 and <= 3))
        {
            throw new InvalidOperationException("Stage must be between 0 and 3.");
        }

        var attendance = dbContext.TeacherSessionAttendances
            .AsNoTracking()
            .Include(x => x.Teacher)
            .Include(x => x.Course)
            .Where(x => x.SessionDate >= query.FromDate && x.SessionDate <= query.ToDate);

        if (query.TeacherId.HasValue)
        {
            attendance = attendance.Where(x => x.TeacherId == query.TeacherId.Value);
        }

        if (query.Grade.HasValue)
        {
            attendance = attendance.Where(x => x.Course != null && x.Course.Grade == query.Grade.Value);
        }

        var rows = await attendance.ToListAsync(cancellationToken);
        if (query.Stage.HasValue)
        {
            rows = rows
                .Where(x => GradeStageHelper.StageCodeForGrade(x.Course?.Grade) == query.Stage.Value)
                .ToList();
        }

        var teacherIds = rows.Select(x => x.TeacherId).Distinct().ToList();
        var rates = await dbContext.TeacherCourseRates
            .AsNoTracking()
            .Where(x => teacherIds.Contains(x.TeacherId))
            .ToListAsync(cancellationToken);

        var rateLookup = rates
            .GroupBy(x => x.TeacherId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(x => x.CourseId, x => x));

        var reportRows = rows
            .GroupBy(x => x.TeacherId)
            .Select(g =>
            {
                var teacher = g.First().Teacher;
                var teacherName = teacher?.DisplayName ?? string.Empty;
                var teacherRates = rateLookup.GetValueOrDefault(g.Key);
                var primary = 0;
                var prep = 0;
                var secondary = 0;
                decimal total = 0m;

                foreach (var row in g)
                {
                    var stage = GradeStageHelper.StageForGrade(row.Course?.Grade);
                    var amount = ResolveSessionAmount(
                        teacher,
                        row.CourseId,
                        stage,
                        teacherRates);

                    switch (stage)
                    {
                        case GradeStage.Primary:
                            primary++;
                            total += amount;
                            break;
                        case GradeStage.Middle:
                            prep++;
                            total += amount;
                            break;
                        case GradeStage.Secondary:
                            secondary++;
                            total += amount;
                            break;
                        default:
                            // KG / unknown: count not shown in stage columns; still add amount if any override.
                            total += amount;
                            break;
                    }
                }

                return new TeacherPayrollRowDto(
                    g.Key,
                    teacherName,
                    primary,
                    prep,
                    secondary,
                    Math.Round(total, 2, MidpointRounding.AwayFromZero));
            })
            .OrderBy(x => x.TeacherName)
            .ToList();

        var grandTotal = Math.Round(
            reportRows.Sum(x => x.TotalAmount),
            2,
            MidpointRounding.AwayFromZero);

        return new TeacherPayrollReportDto(query.FromDate, query.ToDate, reportRows, grandTotal);
    }

    private static decimal ResolveSessionAmount(
        Domain.Entities.User? teacher,
        Guid courseId,
        GradeStage? stage,
        Dictionary<Guid, Domain.Entities.TeacherCourseRate>? teacherRates)
    {
        if (teacherRates is not null
            && teacherRates.TryGetValue(courseId, out var rate)
            && rate.SessionAmount is not null)
        {
            return rate.SessionAmount.Value;
        }

        return stage switch
        {
            GradeStage.Primary => teacher?.PrimaryAmount ?? 0m,
            GradeStage.Middle => teacher?.PrepAmount ?? 0m,
            GradeStage.Secondary => teacher?.SecondaryAmount ?? 0m,
            _ => 0m
        };
    }
}
