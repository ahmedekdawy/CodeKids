namespace CodeKids.Application.Features.Admin;

public sealed record AdminWhatsAppRecipientDto(
    string Phone,
    bool Sent,
    string Detail);

public sealed record SendAdminWhatsAppResultDto(
    int SentCount,
    int FailedCount,
    IReadOnlyList<AdminWhatsAppRecipientDto> Recipients,
    string ShareUrl);
