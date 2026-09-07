using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Admin;

public sealed record SendAdminWhatsAppRequest(
    string Message,
    bool SendToGroup = false,
    IReadOnlyList<string>? Phones = null,
    string? GroupId = null);

public sealed record SendAdminWhatsAppCommand(
    Guid AdminUserId,
    string Message,
    bool SendToGroup,
    IReadOnlyList<string>? Phones,
    string? GroupId) : ICommand<SendAdminWhatsAppResultDto>;
