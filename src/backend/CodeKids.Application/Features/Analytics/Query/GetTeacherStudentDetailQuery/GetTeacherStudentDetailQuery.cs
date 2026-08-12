using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Analytics;

public sealed record GetTeacherStudentDetailQuery(Guid TeacherUserId, Guid StudentId)
    : IQuery<TeacherStudentDetailDto>;
