using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Admin;

public sealed record SendAdminWhatsAppRequest(
    IReadOnlyList<string> Phones,
    string Message);

public sealed record SendAdminWhatsAppCommand(
    Guid AdminUserId,
    IReadOnlyList<string> Phones,
    string Message) : ICommand<SendAdminWhatsAppResultDto>;
