using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.GradeCertificates;

internal static class GradeCertificateAccess
{
    /// <summary>Courses the teacher teaches in the classroom; these are the only subjects they may see or mark.</summary>
    internal static async Task<HashSet<Guid>> GetTeacherCourseIdsAsync(
        IAppDbContext dbContext,
        Guid teacherId,
        Guid classroomId,
        CancellationToken cancellationToken)
    {
        var ids = await dbContext.ClassroomCourses
            .AsNoTracking()
            .Where(x => x.ClassroomId == classroomId && x.TeacherId == teacherId)
            .Select(x => x.CourseId)
            .ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }
}

public sealed class ListGradeCertificatesQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<ListGradeCertificatesQuery, IReadOnlyList<GradeCertificateListItemDto>>
{
    public async Task<IReadOnlyList<GradeCertificateListItemDto>> Handle(
        ListGradeCertificatesQuery query,
        CancellationToken cancellationToken)
    {
        var certificates = dbContext.GradeCertificates.AsNoTracking();
        if (!query.IsAdmin)
        {
            // A teacher sees a certificate only when it has a subject they teach in that classroom.
            var links = dbContext.ClassroomCourses;
            certificates = certificates.Where(certificate => certificate.Subjects.Any(subject =>
                links.Any(link =>
                    link.ClassroomId == certificate.ClassroomId
                    && link.CourseId == subject.CourseId
                    && link.TeacherId == query.UserId)));
        }

        return await certificates
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new GradeCertificateListItemDto(
                x.Id,
                x.Title,
                x.ClassroomId,
                x.Classroom!.Name,
                x.Classroom.Grade,
                x.Subjects.Count,
                x.Classroom.Students.Count,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}

public sealed class GetGradeCertificateSheetQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetGradeCertificateSheetQuery, GradeCertificateSheetDto>
{
    public async Task<GradeCertificateSheetDto> Handle(
        GetGradeCertificateSheetQuery query,
        CancellationToken cancellationToken)
    {
        var certificate = await dbContext.GradeCertificates
            .AsNoTracking()
            .Include(x => x.Classroom)
            .Include(x => x.Subjects).ThenInclude(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == query.CertificateId, cancellationToken)
            ?? throw new InvalidOperationException("Certificate not found.");

        var subjects = certificate.Subjects
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Course?.Title)
            .ToList();
        if (!query.IsAdmin)
        {
            var teacherCourses = await GradeCertificateAccess.GetTeacherCourseIdsAsync(
                dbContext, query.UserId, certificate.ClassroomId, cancellationToken);
            subjects = subjects.Where(x => teacherCourses.Contains(x.CourseId)).ToList();
            if (subjects.Count == 0)
            {
                throw new UnauthorizedAccessException("You do not teach any subject on this certificate.");
            }
        }

        var subjectIds = subjects.Select(x => x.Id).ToList();
        var students = await dbContext.ClassroomStudents
            .AsNoTracking()
            .Where(x => x.ClassroomId == certificate.ClassroomId && x.Student!.Role == UserRole.Student)
            .Select(x => new { x.StudentId, x.Student!.DisplayName })
            .Distinct()
            .ToListAsync(cancellationToken);
        var marks = await dbContext.GradeCertificateMarks
            .AsNoTracking()
            .Where(x => subjectIds.Contains(x.SubjectId))
            .Select(x => new { x.SubjectId, x.StudentId, x.Degree })
            .ToListAsync(cancellationToken);
        var marksByStudent = marks.ToLookup(x => x.StudentId);

        return new GradeCertificateSheetDto(
            certificate.Id,
            certificate.Title,
            certificate.ClassroomId,
            certificate.Classroom?.Name ?? string.Empty,
            certificate.Classroom?.Grade,
            subjects
                .Select(x => new GradeCertificateSubjectDto(
                    x.Id, x.CourseId, x.Course?.Title ?? string.Empty, x.MaxDegree, x.IncludedInTotal, x.SortOrder))
                .ToList(),
            students
                .OrderBy(x => x.DisplayName)
                .Select(x => new GradeCertificateStudentDto(
                    x.StudentId,
                    x.DisplayName,
                    marksByStudent[x.StudentId]
                        .Select(mark => new GradeCertificateMarkDto(mark.SubjectId, mark.Degree))
                        .ToList()))
                .ToList());
    }
}

public sealed class SaveGradeCertificateCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<SaveGradeCertificateCommand, GradeCertificateSheetDto>
{
    private const decimal MaxAllowedDegree = 10000m;

    public async Task<GradeCertificateSheetDto> Handle(
        SaveGradeCertificateCommand command,
        CancellationToken cancellationToken)
    {
        var title = (command.Title ?? string.Empty).Trim();
        if (title.Length == 0)
        {
            throw new InvalidOperationException("Certificate title is required.");
        }

        if (title.Length > 200)
        {
            title = title[..200];
        }

        var inputs = command.Subjects ?? [];
        if (inputs.Count == 0)
        {
            throw new InvalidOperationException("Add at least one subject to the certificate.");
        }

        if (inputs.Select(x => x.CourseId).Distinct().Count() != inputs.Count)
        {
            throw new InvalidOperationException("Each subject can be added to the certificate only once.");
        }

        if (inputs.Any(x => x.MaxDegree <= 0 || x.MaxDegree > MaxAllowedDegree))
        {
            throw new InvalidOperationException("Each subject needs a maximum degree greater than zero.");
        }

        var courseIds = inputs.Select(x => x.CourseId).ToList();
        var knownCourses = await dbContext.Courses
            .AsNoTracking()
            .CountAsync(x => courseIds.Contains(x.Id), cancellationToken);
        if (knownCourses != courseIds.Count)
        {
            throw new InvalidOperationException("One or more subjects were not found.");
        }

        GradeCertificate certificate;
        if (command.CertificateId is Guid id)
        {
            certificate = await dbContext.GradeCertificates
                .Include(x => x.Subjects)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("Certificate not found.");
        }
        else
        {
            if (!await dbContext.Classrooms.AnyAsync(x => x.Id == command.ClassroomId, cancellationToken))
            {
                throw new InvalidOperationException("Classroom not found.");
            }

            certificate = new GradeCertificate
            {
                Id = Guid.NewGuid(),
                ClassroomId = command.ClassroomId,
                CreatedByUserId = command.UserId,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            dbContext.GradeCertificates.Add(certificate);
        }

        certificate.Title = title;

        // Subjects dropped from the certificate take their entered degrees with them.
        var wanted = courseIds.ToHashSet();
        foreach (var removed in certificate.Subjects.Where(x => !wanted.Contains(x.CourseId)).ToList())
        {
            certificate.Subjects.Remove(removed);
            dbContext.GradeCertificateSubjects.Remove(removed);
        }

        var existing = certificate.Subjects.ToDictionary(x => x.CourseId);
        for (var index = 0; index < inputs.Count; index++)
        {
            var input = inputs[index];
            if (!existing.TryGetValue(input.CourseId, out var subject))
            {
                subject = new GradeCertificateSubject
                {
                    Id = Guid.NewGuid(),
                    CertificateId = certificate.Id,
                    CourseId = input.CourseId
                };
                dbContext.GradeCertificateSubjects.Add(subject);
            }
            else if (input.MaxDegree < subject.MaxDegree
                && await dbContext.GradeCertificateMarks.AnyAsync(
                    x => x.SubjectId == subject.Id && x.Degree > input.MaxDegree, cancellationToken))
            {
                throw new InvalidOperationException(
                    "A subject's maximum degree cannot be lower than a degree already entered for a student.");
            }

            subject.MaxDegree = input.MaxDegree;
            subject.IncludedInTotal = input.IncludedInTotal;
            subject.SortOrder = index + 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await new GetGradeCertificateSheetQueryHandler(dbContext).Handle(
            new GetGradeCertificateSheetQuery(certificate.Id, command.UserId, IsAdmin: true),
            cancellationToken);
    }
}

public sealed class DeleteGradeCertificateCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteGradeCertificateCommand, bool>
{
    public async Task<bool> Handle(DeleteGradeCertificateCommand command, CancellationToken cancellationToken)
    {
        var certificate = await dbContext.GradeCertificates
            .FirstOrDefaultAsync(x => x.Id == command.CertificateId, cancellationToken)
            ?? throw new InvalidOperationException("Certificate not found.");

        dbContext.GradeCertificates.Remove(certificate);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class SaveGradeCertificateMarksCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<SaveGradeCertificateMarksCommand, GradeCertificateSheetDto>
{
    public async Task<GradeCertificateSheetDto> Handle(
        SaveGradeCertificateMarksCommand command,
        CancellationToken cancellationToken)
    {
        var certificate = await dbContext.GradeCertificates
            .AsNoTracking()
            .Include(x => x.Subjects)
            .FirstOrDefaultAsync(x => x.Id == command.CertificateId, cancellationToken)
            ?? throw new InvalidOperationException("Certificate not found.");

        var editable = certificate.Subjects.ToDictionary(x => x.Id);
        if (!command.IsAdmin)
        {
            var teacherCourses = await GradeCertificateAccess.GetTeacherCourseIdsAsync(
                dbContext, command.UserId, certificate.ClassroomId, cancellationToken);
            editable = editable
                .Where(x => teacherCourses.Contains(x.Value.CourseId))
                .ToDictionary(x => x.Key, x => x.Value);
        }

        var entries = command.Entries ?? [];
        var studentIds = (await dbContext.ClassroomStudents
            .AsNoTracking()
            .Where(x => x.ClassroomId == certificate.ClassroomId)
            .Select(x => x.StudentId)
            .ToListAsync(cancellationToken)).ToHashSet();

        foreach (var entry in entries)
        {
            if (!editable.TryGetValue(entry.SubjectId, out var subject))
            {
                throw new UnauthorizedAccessException("You can only enter degrees for your own subjects.");
            }

            if (!studentIds.Contains(entry.StudentId))
            {
                throw new InvalidOperationException("One or more students are not in this classroom.");
            }

            if (entry.Degree is decimal degree && (degree < 0 || degree > subject.MaxDegree))
            {
                throw new InvalidOperationException(
                    $"A degree must be between 0 and the subject maximum ({subject.MaxDegree:0.##}).");
            }
        }

        var subjectIds = entries.Select(x => x.SubjectId).Distinct().ToList();
        var existing = (await dbContext.GradeCertificateMarks
            .Where(x => subjectIds.Contains(x.SubjectId))
            .ToListAsync(cancellationToken))
            .ToDictionary(x => (x.SubjectId, x.StudentId));

        var now = DateTimeOffset.UtcNow;
        foreach (var entry in entries)
        {
            var key = (entry.SubjectId, entry.StudentId);
            existing.TryGetValue(key, out var mark);

            // An empty degree clears the mark.
            if (entry.Degree is not decimal degree)
            {
                if (mark is not null)
                {
                    dbContext.GradeCertificateMarks.Remove(mark);
                    existing.Remove(key);
                }

                continue;
            }

            if (mark is null)
            {
                mark = new GradeCertificateMark
                {
                    Id = Guid.NewGuid(),
                    SubjectId = entry.SubjectId,
                    StudentId = entry.StudentId
                };
                dbContext.GradeCertificateMarks.Add(mark);
                existing[key] = mark;
            }
            else if (mark.Degree == degree)
            {
                continue;
            }

            mark.Degree = degree;
            mark.UpdatedByUserId = command.UserId;
            mark.UpdatedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await new GetGradeCertificateSheetQueryHandler(dbContext).Handle(
            new GetGradeCertificateSheetQuery(certificate.Id, command.UserId, command.IsAdmin),
            cancellationToken);
    }
}
