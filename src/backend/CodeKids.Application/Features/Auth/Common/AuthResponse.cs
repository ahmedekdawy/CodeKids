using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CodeKids.Application.Features.Auth;

public sealed record AuthResponse(string Token, AuthUserDto User);
