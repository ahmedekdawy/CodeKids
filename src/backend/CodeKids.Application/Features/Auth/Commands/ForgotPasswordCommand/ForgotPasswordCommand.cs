using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Auth;

public sealed record ForgotPasswordRequest(string Email);

public sealed record ForgotPasswordCommand(string EmailOrMobile) : ICommand<ForgotPasswordResult>;
