using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Courses;

public sealed record CourseQuizDto(Guid Id, string Title, string Description, int XpReward, int QuestionCount);
