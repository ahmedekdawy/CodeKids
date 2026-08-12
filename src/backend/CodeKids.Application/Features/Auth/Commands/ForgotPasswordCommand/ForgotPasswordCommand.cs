using System.Security.Cryptography;
using System.Text;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Options;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeKids.Application.Features.Auth;

public sealed record ForgotPasswordRequest(string Email);

public sealed record ForgotPasswordCommand(string EmailOrMobile) : ICommand<ForgotPasswordResult>;
