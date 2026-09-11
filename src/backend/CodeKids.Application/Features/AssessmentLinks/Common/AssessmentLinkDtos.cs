using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Auth;

namespace CodeKids.Application.Features.AssessmentLinks;

public sealed record AssessmentStudentLinkDto(
    Guid StudentId,
    string DisplayName,
    string Email,
    string? MobilePhone,
    string Key,
    string Path);

public sealed record AssessmentStudentLinksResult(
    AssessmentLinkKind Kind,
    Guid ResourceId,
    string Title,
    IReadOnlyList<AssessmentStudentLinkDto> Links);

public sealed record RedeemAssessmentLinkResult(
    string Token,
    AuthUserDto User,
    string RedirectPath,
    string Kind,
    Guid ResourceId);
