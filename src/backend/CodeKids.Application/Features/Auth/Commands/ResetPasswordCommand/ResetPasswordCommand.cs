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

public sealed record ResetPasswordRequest(string Token, string NewPassword);

public sealed record ResetPasswordCommand(string Token, string NewPassword) : ICommand<bool>;
