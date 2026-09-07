using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Assessments;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.Classrooms;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Exams;

public sealed class GetExamsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetExamsQuery, IReadOnlyList<ExamDto>>
{
    public async Task<IReadOnlyList<ExamDto>> Handle(GetExamsQuery query, CancellationToken cancellationToken)
    {
        var exams = await dbContext.Exams
            .AsNoTracking()
            .Include(x => x.Classroom!)
                .ThenInclude(c => c.Courses)
            .Include(x => x.Classroom!)
                .ThenInclude(c => c.Students)
            .Include(x => x.Course)
            .Include(x => x.CreatedBy)
            .Include(x => x.Questions)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (query.ClassroomId is Guid classroomId)
        {
            exams = exams.Where(x => x.ClassroomId == classroomId).ToList();
        }

        if (!PublishedAssessmentAccess.CanViewUnpublished(query.ViewerRole))
        {
            exams = exams.Where(x => x.IsPublished).ToList();
        }

        var isTeacher = string.Equals(query.ViewerRole, nameof(UserRole.Teacher), StringComparison.OrdinalIgnoreCase);
        var isStudent = string.Equals(query.ViewerRole, nameof(UserRole.Student), StringComparison.OrdinalIgnoreCase);
        var isAdmin = string.Equals(query.ViewerRole, nameof(UserRole.SuperAdmin), StringComparison.OrdinalIgnoreCase);

        if (isTeacher)
        {
            exams = exams.Where(x => x.Classroom?.Courses.Any(t => t.TeacherId == query.ViewerUserId) == true).ToList();
        }
        else if (isStudent)
        {
            var visibleCourseIds = await StudentCourseVisibility.GetVisibleCourseIdsAsync(
                dbContext, query.ViewerUserId, cancellationToken);

            exams = exams
                .Where(x => x.Classroom?.Students.Any(s => s.StudentId == query.ViewerUserId) == true)
                .Where(x => x.CourseId is null || visibleCourseIds.Contains(x.CourseId.Value))
                .ToList();
        }
        else if (!isAdmin)
        {
            exams = [];
        }

        return exams.Select(e => CreateExamCommandHandler.Map(e, includeAnswerKey: isTeacher || isAdmin)).ToList();
    }
}
