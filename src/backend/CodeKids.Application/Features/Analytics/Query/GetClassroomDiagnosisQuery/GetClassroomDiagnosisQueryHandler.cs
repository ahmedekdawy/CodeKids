using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Analytics;

public sealed class GetClassroomDiagnosisQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetClassroomDiagnosisQuery, ClassroomDiagnosisDto>
{
    public async Task<ClassroomDiagnosisDto> Handle(
        GetClassroomDiagnosisQuery query,
        CancellationToken cancellationToken)
    {
        var classroom = await dbContext.Classrooms
            .AsNoTracking()
            .Include(x => x.Students)
            .FirstOrDefaultAsync(
                x => x.Id == query.ClassroomId && x.Courses.Any(t => t.TeacherId == query.TeacherUserId),
                cancellationToken)
            ?? throw new InvalidOperationException("Classroom not found.");

        var studentIds = classroom.Students.Select(x => x.StudentId).ToList();
        var students = await dbContext.Users
            .AsNoTracking()
            .Where(x => studentIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var weaknesses = await AnalyticsQueries.GetWeakLessonsForClassroom(
            dbContext, studentIds, cancellationToken);

        var avgXp = students.Count == 0 ? 0 : students.Average(x => x.TotalXp);
        var behind = students
            .Where(x => x.TotalXp < avgXp * 0.6 || StudentLevelCalculator.FromXp(x.TotalXp).LevelNumber <= 1)
            .OrderBy(x => x.TotalXp)
            .Select(x => x.DisplayName)
            .Take(8)
            .ToList();
        var strong = students
            .OrderByDescending(x => x.TotalXp)
            .Take(5)
            .Select(x => x.DisplayName)
            .ToList();

        return new ClassroomDiagnosisDto(
            classroom.Id,
            classroom.Name,
            weaknesses.Take(8).ToList(),
            behind,
            strong);
    }
}
