using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.AssessmentLinks;

public sealed record RedeemAssessmentLinkCommand(string Key) : ICommand<RedeemAssessmentLinkResult>;
