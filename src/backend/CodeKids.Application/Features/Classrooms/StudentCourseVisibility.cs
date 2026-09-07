using CodeKids.Application.Abstractions;
using CodeKids.Domain;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Classrooms;

public static class StudentCourseVisibility
{
    /// <summary>
    /// Per classroom: specific course enrollments if any, otherwise all classroom courses
    /// that match the student grade and school type (or all-grades / all-school-type courses).
    /// </summary>
    public static async Task<HashSet<Guid>> GetVisibleCourseIdsAsync(
        IAppDbContext dbContext,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var classroomIds = await dbContext.ClassroomStudents
            .AsNoTracking()
            .Where(x => x.StudentId == studentId)
            .Select(x => x.ClassroomId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (classroomIds.Count == 0)
        {
            return [];
        }

        var student = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == studentId)
            .Select(x => new { x.Grade, x.SchoolType })
            .FirstOrDefaultAsync(cancellationToken);

        var specific = await dbContext.StudentCourseEnrollments
            .AsNoTracking()
            .Where(x => x.StudentId == studentId && classroomIds.Contains(x.ClassroomId))
            .Select(x => new { x.ClassroomId, x.CourseId })
            .ToListAsync(cancellationToken);

        var specificByClassroom = specific
            .GroupBy(x => x.ClassroomId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.CourseId).ToHashSet());

        var assigned = await (
            from cc in dbContext.ClassroomCourses.AsNoTracking()
            join course in dbContext.Courses.AsNoTracking() on cc.CourseId equals course.Id
            where classroomIds.Contains(cc.ClassroomId) && course.IsPublished
            select new { cc.ClassroomId, cc.CourseId, course.Grade, course.StageId, course.SchoolType }
        ).ToListAsync(cancellationToken);

        var legacy = await (
            from room in dbContext.Classrooms.AsNoTracking()
            join course in dbContext.Courses.AsNoTracking() on room.CourseId equals course.Id
            where classroomIds.Contains(room.Id) && room.CourseId != null && course.IsPublished
            select new { ClassroomId = room.Id, CourseId = course.Id, course.Grade, course.StageId, course.SchoolType }
        ).ToListAsync(cancellationToken);

        var courseMeta = assigned
            .Concat(legacy)
            .GroupBy(x => x.CourseId)
            .ToDictionary(
                g => g.Key,
                g => (g.First().Grade, g.First().StageId, g.First().SchoolType));

        var missingIds = specific
            .Select(x => x.CourseId)
            .Where(id => !courseMeta.ContainsKey(id))
            .Distinct()
            .ToList();
        if (missingIds.Count > 0)
        {
            var extra = await dbContext.Courses
                .AsNoTracking()
                .Where(c => missingIds.Contains(c.Id) && c.IsPublished)
                .Select(c => new { c.Id, c.Grade, c.StageId, c.SchoolType })
                .ToListAsync(cancellationToken);
            foreach (var row in extra)
            {
                courseMeta[row.Id] = (row.Grade, row.StageId, row.SchoolType);
            }
        }

        var visible = new HashSet<Guid>();
        foreach (var classroomId in classroomIds)
        {
            if (specificByClassroom.TryGetValue(classroomId, out var enrolled) && enrolled.Count > 0)
            {
                foreach (var courseId in enrolled)
                {
                    if (courseMeta.TryGetValue(courseId, out var meta)
                        && MatchesStudent(meta.Grade, meta.StageId, student?.Grade, meta.SchoolType, student?.SchoolType))
                    {
                        visible.Add(courseId);
                    }
                }

                continue;
            }

            foreach (var row in assigned.Where(x => x.ClassroomId == classroomId))
            {
                if (MatchesStudent(row.Grade, row.StageId, student?.Grade, row.SchoolType, student?.SchoolType))
                {
                    visible.Add(row.CourseId);
                }
            }

            foreach (var row in legacy.Where(x => x.ClassroomId == classroomId))
            {
                if (MatchesStudent(row.Grade, row.StageId, student?.Grade, row.SchoolType, student?.SchoolType))
                {
                    visible.Add(row.CourseId);
                }
            }
        }

        return visible;
    }

    public static bool MatchesStudent(
        int? courseGrade,
        int? courseStageId,
        int? studentGrade,
        SchoolType? courseSchoolType,
        SchoolType? studentSchoolType) =>
        GradeStageHelper.CourseCoversGrade(courseGrade, courseStageId, studentGrade)
        && MatchesStudentSchoolType(courseSchoolType, studentSchoolType);

    public static bool MatchesStudentGrade(int? courseGrade, int? studentGrade) =>
        GradeStageHelper.CourseCoversGrade(courseGrade, null, studentGrade);

    public static bool MatchesStudentSchoolType(SchoolType? courseSchoolType, SchoolType? studentSchoolType) =>
        courseSchoolType is null
        || courseSchoolType == SchoolType.All
        || studentSchoolType is null
        || studentSchoolType == SchoolType.All
        || courseSchoolType == studentSchoolType;

    public static HashSet<Guid> EnrolledCourseIdsForClassroom(
        IEnumerable<StudentCourseEnrollment> enrollments,
        Guid studentId,
        Guid classroomId) =>
        enrollments
            .Where(x => x.StudentId == studentId && x.ClassroomId == classroomId)
            .Select(x => x.CourseId)
            .ToHashSet();
}
