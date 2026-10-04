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
    DateTimeOffset CreatedAtUtc,
    bool IsApproved,
    DateTimeOffset? ApprovedAtUtc);

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
    bool IsApproved,
    DateTimeOffset? ApprovedAtUtc,
    IReadOnlyList<GradeCertificateSubjectDto> Subjects,
    IReadOnlyList<GradeCertificateStudentDto> Students);

/// <summary>An approved certificate as seen by one student or their parent, with that student's degrees.</summary>
public sealed record StudentGradeCertificateDto(
    Guid Id,
    string Title,
    Guid ClassroomId,
    string ClassroomName,
    int? Grade,
    DateTimeOffset ApprovedAtUtc,
    IReadOnlyList<GradeCertificateSubjectDto> Subjects,
    IReadOnlyList<GradeCertificateMarkDto> Marks);

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

/// <summary>Approves one certificate so the student and their parent can see it. Returns 1 when it changed.</summary>
public sealed record ApproveGradeCertificateCommand(Guid CertificateId, Guid UserId) : ICommand<int>;

/// <summary>Removes the approval again. Returns 1 when it changed.</summary>
public sealed record RevokeGradeCertificateApprovalCommand(Guid CertificateId) : ICommand<int>;

/// <summary>Approves every not-yet-approved certificate of the tenant. Returns how many were approved.</summary>
public sealed record ApproveAllGradeCertificatesCommand(Guid UserId) : ICommand<int>;

/// <summary>Approved certificates of one student, with that student's degrees.</summary>
public sealed record ListStudentGradeCertificatesQuery(Guid StudentId)
    : IQuery<IReadOnlyList<StudentGradeCertificateDto>>;

/// <summary>Same as <see cref="ListStudentGradeCertificatesQuery"/>, but the parent link is verified first.</summary>
public sealed record ListChildGradeCertificatesQuery(Guid ParentId, Guid ChildId)
    : IQuery<IReadOnlyList<StudentGradeCertificateDto>>;
