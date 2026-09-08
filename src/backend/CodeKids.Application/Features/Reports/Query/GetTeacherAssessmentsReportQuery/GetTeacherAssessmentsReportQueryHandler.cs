using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Reports;

public sealed class GetTeacherAssessmentsReportQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetTeacherAssessmentsReportQuery, PagedTeacherAssessmentsResultDto>
{
    public async Task<PagedTeacherAssessmentsResultDto> Handle(
        GetTeacherAssessmentsReportQuery query,
        CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Max(1, query.Page);
        var status = (query.Status ?? string.Empty).Trim().ToLowerInvariant();
        var kind = (query.Kind ?? string.Empty).Trim().ToLowerInvariant();
        var includeAssignments = string.IsNullOrEmpty(kind) || kind is "assignment" or "assignments";
        var includeQuizzes = string.IsNullOrEmpty(kind) || kind is "quiz" or "quizzes";
        var includeExams = string.IsNullOrEmpty(kind) || kind is "exam" or "exams";

        DateTimeOffset? fromUtc = query.FromDate is DateOnly from
            ? new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
            : null;
        DateTimeOffset? toUtcExclusive = query.ToDate is DateOnly to
            ? new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
            : null;

        bool? published = status switch
        {
            "published" => true,
            "draft" or "unpublished" => false,
            _ => null
        };

        var items = new List<TeacherAssessmentReportItemDto>();

        if (includeAssignments)
        {
            var assignments = dbContext.Assignments.AsNoTracking().AsQueryable();

            if (query.TeacherId is Guid teacherId)
            {
                assignments = assignments.Where(x => x.CreatedByUserId == teacherId);
            }

            if (query.ClassroomId is Guid classroomId)
            {
                assignments = assignments.Where(x => x.ClassroomId == classroomId);
            }

            if (fromUtc is DateTimeOffset fromValue)
            {
                assignments = assignments.Where(x => x.CreatedAtUtc >= fromValue);
            }

            if (toUtcExclusive is DateTimeOffset toValue)
            {
                assignments = assignments.Where(x => x.CreatedAtUtc < toValue);
            }

            if (published is bool isPublished)
            {
                assignments = assignments.Where(x => x.IsPublished == isPublished);
            }

            items.AddRange(await assignments
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new TeacherAssessmentReportItemDto(
                    "Assignment",
                    x.Id,
                    x.Title,
                    x.CreatedByUserId,
                    x.CreatedBy != null ? x.CreatedBy.DisplayName : "",
                    x.ClassroomId,
                    x.Classroom != null ? x.Classroom.Name : "",
                    null,
                    x.CreatedAtUtc,
                    x.IsPublished))
                .ToListAsync(cancellationToken));
        }

        if (includeQuizzes)
        {
            var quizzes = dbContext.Quizzes.AsNoTracking().AsQueryable();

            if (query.TeacherId is Guid teacherId)
            {
                quizzes = quizzes.Where(x => x.CreatedByUserId == teacherId);
            }

            if (query.ClassroomId is Guid classroomId)
            {
                quizzes = quizzes.Where(x => x.ClassroomId == classroomId);
            }

            if (fromUtc is DateTimeOffset fromValue)
            {
                quizzes = quizzes.Where(x => x.CreatedAtUtc >= fromValue);
            }

            if (toUtcExclusive is DateTimeOffset toValue)
            {
                quizzes = quizzes.Where(x => x.CreatedAtUtc < toValue);
            }

            if (published is bool isPublished)
            {
                quizzes = quizzes.Where(x => x.IsPublished == isPublished);
            }

            items.AddRange(await quizzes
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new TeacherAssessmentReportItemDto(
                    "Quiz",
                    x.Id,
                    x.Title,
                    x.CreatedByUserId,
                    x.CreatedBy != null ? x.CreatedBy.DisplayName : "",
                    x.ClassroomId,
                    x.Classroom != null ? x.Classroom.Name : "",
                    x.Course != null ? x.Course.Title : null,
                    x.CreatedAtUtc,
                    x.IsPublished))
                .ToListAsync(cancellationToken));
        }

        if (includeExams)
        {
            var exams = dbContext.Exams.AsNoTracking().AsQueryable();

            if (query.TeacherId is Guid teacherId)
            {
                exams = exams.Where(x => x.CreatedByUserId == teacherId);
            }

            if (query.ClassroomId is Guid classroomId)
            {
                exams = exams.Where(x => x.ClassroomId == classroomId);
            }

            if (fromUtc is DateTimeOffset fromValue)
            {
                exams = exams.Where(x => x.CreatedAtUtc >= fromValue);
            }

            if (toUtcExclusive is DateTimeOffset toValue)
            {
                exams = exams.Where(x => x.CreatedAtUtc < toValue);
            }

            if (published is bool isPublished)
            {
                exams = exams.Where(x => x.IsPublished == isPublished);
            }

            items.AddRange(await exams
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new TeacherAssessmentReportItemDto(
                    "Exam",
                    x.Id,
                    x.Title,
                    x.CreatedByUserId,
                    x.CreatedBy != null ? x.CreatedBy.DisplayName : "",
                    x.ClassroomId,
                    x.Classroom != null ? x.Classroom.Name : "",
                    x.Course != null ? x.Course.Title : null,
                    x.CreatedAtUtc,
                    x.IsPublished))
                .ToListAsync(cancellationToken));
        }

        var totalCount = items.Count;
        if (totalCount == 0)
        {
            return new PagedTeacherAssessmentsResultDto([], 0, page, pageSize);
        }

        var maxPage = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        if (page > maxPage)
        {
            page = maxPage;
        }

        var pageItems = items
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenBy(x => x.Kind, StringComparer.Ordinal)
            .ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedTeacherAssessmentsResultDto(pageItems, totalCount, page, pageSize);
    }
}
