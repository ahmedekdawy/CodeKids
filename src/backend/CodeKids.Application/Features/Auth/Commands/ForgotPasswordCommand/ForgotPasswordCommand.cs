using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Auth;

public sealed record ForgotPasswordRequest(string Email, string? Channel = null);

public sealed record ForgotPasswordCommand(string EmailOrMobile, string Channel = "whatsapp")
    : ICommand<ForgotPasswordResult>;
