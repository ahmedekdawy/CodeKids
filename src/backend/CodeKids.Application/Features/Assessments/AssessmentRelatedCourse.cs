using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Assessments;

internal static class AssessmentRelatedCourse
{
    /// <summary>
    /// Picks the single course that belongs to this assessment. Never falls back to a classroom's
    /// leftover primary course or to every subject taught in the room.
    /// </summary>
    internal static async Task<Guid?> ResolveIdAsync(
        IAppDbContext dbContext,
        Guid? explicitCourseId,
        Guid? classroomId,
        Guid? teacherId,
        CancellationToken cancellationToken)
    {
        if (explicitCourseId is Guid requested && requested != Guid.Empty)
        {
            if (classroomId is Guid scopedClassroom)
            {
                var belongs = await dbContext.ClassroomCourses
                    .AsNoTracking()
                    .AnyAsync(
                        x => x.ClassroomId == scopedClassroom && x.CourseId == requested,
                        cancellationToken);
                return belongs ? requested : null;
            }

            return requested;
        }

        if (classroomId is not Guid classroom)
        {
            return null;
        }

        var links = await dbContext.ClassroomCourses
            .AsNoTracking()
            .Where(x => x.ClassroomId == classroom)
            .Select(x => new { x.CourseId, x.TeacherId })
            .ToListAsync(cancellationToken);

        if (teacherId is Guid teacher)
        {
            var taught = links
                .Where(x => x.TeacherId == teacher)
                .Select(x => x.CourseId)
                .Distinct()
                .ToList();
            if (taught.Count == 1)
            {
                return taught[0];
            }

            if (taught.Count > 1)
            {
                return null;
            }
        }

        var distinct = links.Select(x => x.CourseId).Distinct().ToList();
        return distinct.Count == 1 ? distinct[0] : null;
    }
}
