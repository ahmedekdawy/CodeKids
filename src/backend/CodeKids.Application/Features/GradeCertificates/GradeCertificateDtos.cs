using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.GradeCertificates;

public sealed record GradeCertificateListItemDto(
    Guid Id,
    string Title,
    Guid ClassroomId,
    string ClassroomName,
    int? Grade,
    int SubjectCount,
    int StudentCount,
    DateTimeOffset CreatedAtUtc);

public sealed record GradeCertificateSubjectDto(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    decimal MaxDegree,
    bool IncludedInTotal,
    int SortOrder);

public sealed record GradeCertificateMarkDto(Guid SubjectId, decimal Degree);

public sealed record GradeCertificateStudentDto(
    Guid StudentId,
    string StudentName,
    IReadOnlyList<GradeCertificateMarkDto> Marks);

/// <summary>Certificate with its subjects and every student's degrees. Teachers only get their own subjects.</summary>
public sealed record GradeCertificateSheetDto(
    Guid Id,
    string Title,
    Guid ClassroomId,
    string ClassroomName,
    int? Grade,
    IReadOnlyList<GradeCertificateSubjectDto> Subjects,
    IReadOnlyList<GradeCertificateStudentDto> Students);

public sealed record SaveGradeCertificateSubjectInput(Guid CourseId, decimal MaxDegree, bool IncludedInTotal);

public sealed record SaveGradeCertificateRequest(
    Guid ClassroomId,
    string Title,
    IReadOnlyList<SaveGradeCertificateSubjectInput> Subjects);

public sealed record SaveGradeCertificateMarkInput(Guid SubjectId, Guid StudentId, decimal? Degree);

public sealed record SaveGradeCertificateMarksRequest(IReadOnlyList<SaveGradeCertificateMarkInput> Entries);

public sealed record ListGradeCertificatesQuery(Guid UserId, bool IsAdmin)
    : IQuery<IReadOnlyList<GradeCertificateListItemDto>>;

public sealed record GetGradeCertificateSheetQuery(Guid CertificateId, Guid UserId, bool IsAdmin)
    : IQuery<GradeCertificateSheetDto>;

/// <summary>Creates the certificate when <paramref name="CertificateId"/> is null, otherwise updates it.</summary>
public sealed record SaveGradeCertificateCommand(
    Guid? CertificateId,
    Guid UserId,
    Guid ClassroomId,
    string Title,
    IReadOnlyList<SaveGradeCertificateSubjectInput> Subjects) : ICommand<GradeCertificateSheetDto>;

public sealed record DeleteGradeCertificateCommand(Guid CertificateId) : ICommand<bool>;

public sealed record SaveGradeCertificateMarksCommand(
    Guid CertificateId,
    Guid UserId,
    bool IsAdmin,
    IReadOnlyList<SaveGradeCertificateMarkInput> Entries) : ICommand<GradeCertificateSheetDto>;
