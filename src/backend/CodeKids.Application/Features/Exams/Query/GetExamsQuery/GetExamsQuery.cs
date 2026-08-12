using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Badges;
using CodeKids.Application.Features.QuestionBank;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Exams;

public sealed record GetExamsQuery(Guid ViewerUserId, string ViewerRole, Guid? ClassroomId = null)
    : IQuery<IReadOnlyList<ExamDto>>;
