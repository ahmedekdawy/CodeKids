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
            attendance = attendance.Where(x =>
                x.Course != null
                && (x.Course.Grade == query.Grade.Value
                    || (x.Course.Grade == null && x.Course.StageId == null)
                    || (x.Course.Grade == null
                        && x.Course.StageId != null
                        && dbContext.Grades.Any(g => g.Id == query.Grade.Value && g.StageId == x.Course.StageId))));
        }

        var attendanceRows = await attendance.ToListAsync(cancellationToken);
        if (query.Stage.HasValue)
        {
            attendanceRows = attendanceRows
                .Where(x =>
                    x.Course?.StageId == query.Stage.Value
                    || GradeStageHelper.StageCodeForGrade(x.Course?.Grade) == query.Stage.Value
                    || (x.Course?.Grade == null && x.Course?.StageId == null))
                .ToList();
        }

        var teacherIds = attendanceRows.Select(x => x.TeacherId).Distinct().ToList();

        var adjustmentsQuery = dbContext.TeacherPayrollAdjustments
            .AsNoTracking()
            .Where(x => x.AdjustmentDate >= query.FromDate && x.AdjustmentDate <= query.ToDate);
        if (query.TeacherId.HasValue)
        {
            adjustmentsQuery = adjustmentsQuery.Where(x => x.TeacherId == query.TeacherId.Value);
        }

        var adjustments = await adjustmentsQuery.ToListAsync(cancellationToken);
        teacherIds = teacherIds
            .Concat(adjustments.Select(x => x.TeacherId))
            .Distinct()
            .ToList();

        var teachersQuery = dbContext.Users
            .AsNoTracking()
            .Where(x => x.Role == UserRole.Teacher);

        if (query.TeacherId.HasValue)
        {
            teachersQuery = teachersQuery.Where(x => x.Id == query.TeacherId.Value);
        }
        else if (teacherIds.Count > 0)
        {
            teachersQuery = teachersQuery.Where(x =>
                teacherIds.Contains(x.Id) || (x.MonthlySalary != null && x.MonthlySalary > 0));
        }
        else
        {
            teachersQuery = teachersQuery.Where(x => x.MonthlySalary != null && x.MonthlySalary > 0);
        }

        var teachers = await teachersQuery.ToListAsync(cancellationToken);
        teacherIds = teachers.Select(x => x.Id)
            .Concat(teacherIds)
            .Distinct()
            .ToList();

        var rates = await dbContext.TeacherCourseRates
            .AsNoTracking()
            .Where(x => teacherIds.Contains(x.TeacherId))
            .ToListAsync(cancellationToken);

        var rateLookup = rates
            .GroupBy(x => x.TeacherId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(x => x.CourseId, x => x));

        var sessionByTeacher = attendanceRows
            .GroupBy(x => x.TeacherId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var manualByTeacher = adjustments
            .GroupBy(x => x.TeacherId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var monthsInRange = CountMonths(query.FromDate, query.ToDate);
        var teacherLookup = teachers.ToDictionary(x => x.Id);

        var reportRows = teacherIds
            .Select(teacherId =>
            {
                teacherLookup.TryGetValue(teacherId, out var teacher);
                var teacherName = teacher?.DisplayName
                    ?? attendanceRows.FirstOrDefault(x => x.TeacherId == teacherId)?.Teacher?.DisplayName
                    ?? string.Empty;

                var primary = 0;
                var prep = 0;
                var secondary = 0;
                decimal sessionAmount = 0m;

                if (sessionByTeacher.TryGetValue(teacherId, out var sessions))
                {
                    var teacherRates = rateLookup.GetValueOrDefault(teacherId);
                    foreach (var row in sessions)
                    {
                        var stage = GradeStageHelper.StageForGrade(row.Course?.Grade);
                        var amount = ResolveSessionAmount(
                            teacher ?? row.Teacher,
                            row.CourseId,
                            stage,
                            teacherRates);

                        switch (stage)
                        {
                            case GradeStage.Primary:
                                primary++;
                                sessionAmount += amount;
                                break;
                            case GradeStage.Middle:
                                prep++;
                                sessionAmount += amount;
                                break;
                            case GradeStage.Secondary:
                                secondary++;
                                sessionAmount += amount;
                                break;
                            default:
                                sessionAmount += amount;
                                break;
                        }
                    }
                }

                var monthlySalary = Math.Round(
                    (teacher?.MonthlySalary ?? 0m) * monthsInRange,
                    2,
                    MidpointRounding.AwayFromZero);
                var manualAmount = Math.Round(
                    manualByTeacher.GetValueOrDefault(teacherId),
                    2,
                    MidpointRounding.AwayFromZero);
                sessionAmount = Math.Round(sessionAmount, 2, MidpointRounding.AwayFromZero);
                var totalAmount = Math.Round(
                    sessionAmount + monthlySalary + manualAmount,
                    2,
                    MidpointRounding.AwayFromZero);

                return new TeacherPayrollRowDto(
                    teacherId,
                    teacherName,
                    primary,
                    prep,
                    secondary,
                    sessionAmount,
                    monthlySalary,
                    manualAmount,
                    totalAmount);
            })
            .Where(x => x.SessionAmount != 0 || x.MonthlySalary != 0 || x.ManualAmount != 0)
            .OrderBy(x => x.TeacherName)
            .ToList();

        var grandTotal = Math.Round(
            reportRows.Sum(x => x.TotalAmount),
            2,
            MidpointRounding.AwayFromZero);

        return new TeacherPayrollReportDto(query.FromDate, query.ToDate, reportRows, grandTotal);
    }

    private static int CountMonths(DateOnly fromDate, DateOnly toDate) =>
        (toDate.Year - fromDate.Year) * 12 + (toDate.Month - fromDate.Month) + 1;

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
