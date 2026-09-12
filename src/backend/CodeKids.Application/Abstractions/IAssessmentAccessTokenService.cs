namespace CodeKids.Application.Abstractions;

public enum AssessmentLinkKind : byte
{
    Exam = 1,
    Quiz = 2,
    Assignment = 3
}

public interface IAssessmentAccessTokenService
{
    string CreateToken(AssessmentLinkKind kind, Guid resourceId, Guid studentId, TimeSpan lifetime);

    bool TryValidate(
        string token,
        out AssessmentLinkKind kind,
        out Guid resourceId,
        out Guid studentId,
        out DateTimeOffset expiresAt);
}
