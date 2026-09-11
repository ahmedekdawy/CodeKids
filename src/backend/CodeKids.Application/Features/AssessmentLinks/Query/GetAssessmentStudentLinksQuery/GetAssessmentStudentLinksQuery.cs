using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.AssessmentLinks;

public sealed record GetAssessmentStudentLinksQuery(
    Guid ViewerUserId,
    string ViewerRole,
    AssessmentLinkKind Kind,
    Guid ResourceId) : IQuery<AssessmentStudentLinksResult>;
