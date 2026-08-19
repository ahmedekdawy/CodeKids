using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

public static class StudentGradeResolver
{
    /// <summary>
    /// Uses the student's stored grade when set; otherwise infers it from classroom or course enrollment.
    /// </summary>
    public static async Task<Dictionary<Guid, int?>> ResolveAsync(
        IAppDbContext dbContext,
        IEnumerable<(Guid StudentId, int? Grade)> students,
        CancellationToken cancellationToken)
    {
        var result = students.ToDictionary(x => x.StudentId, x => x.Grade);
        var missing = result.Where(x => x.Value is null).Select(x => x.Key).ToList();
        if (missing.Count == 0)
        {
            return result;
        }

        var classroomRows = await (
            from cs in dbContext.ClassroomStudents.AsNoTracking()
            join room in dbContext.Classrooms.AsNoTracking() on cs.ClassroomId equals room.Id
            where missing.Contains(cs.StudentId)
            select new
            {
                cs.StudentId,
                ClassroomId = room.Id,
                ClassroomGrade = room.Grade,
                room.CourseId
            }).ToListAsync(cancellationToken);

        var classroomIds = classroomRows.Select(x => x.ClassroomId).Distinct().ToList();
        var assignedCourseGrades = await (
            from cc in dbContext.ClassroomCourses.AsNoTracking()
            join course in dbContext.Courses.AsNoTracking() on cc.CourseId equals course.Id
            where classroomIds.Contains(cc.ClassroomId) && course.Grade != null
            select new { cc.ClassroomId, Grade = course.Grade!.Value }
        ).ToListAsync(cancellationToken);

        var legacyCourseIds = classroomRows
            .Where(x => x.CourseId != null)
            .Select(x => x.CourseId!.Value)
            .Distinct()
            .ToList();
        var legacyCourseGrades = await dbContext.Courses
            .AsNoTracking()
            .Where(c => legacyCourseIds.Contains(c.Id) && c.Grade != null)
            .ToDictionaryAsync(c => c.Id, c => c.Grade!.Value, cancellationToken);

        var gradesByClassroom = assignedCourseGrades
            .GroupBy(x => x.ClassroomId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Grade).ToList());

        foreach (var studentId in missing)
        {
            var candidates = new List<int>();
            foreach (var room in classroomRows.Where(x => x.StudentId == studentId))
            {
                if (room.ClassroomGrade is int classroomGrade)
                {
                    candidates.Add(classroomGrade);
                }

                if (gradesByClassroom.TryGetValue(room.ClassroomId, out var courseGrades))
                {
                    candidates.AddRange(courseGrades);
                }

                if (room.CourseId is Guid courseId && legacyCourseGrades.TryGetValue(courseId, out var legacyGrade))
                {
                    candidates.Add(legacyGrade);
                }
            }

            if (candidates.Count > 0)
            {
                result[studentId] = candidates
                    .GroupBy(x => x)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key)
                    .First()
                    .Key;
            }
        }

        return result;
    }
}
