using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

public sealed record DeleteAssignmentSolutionVideoCommand(Guid TeacherUserId, Guid AssignmentId) : ICommand<bool>;
