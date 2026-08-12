using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CodeKids.Application.Features.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginCommand(string Email, string Password) : ICommand<AuthResponse>;
