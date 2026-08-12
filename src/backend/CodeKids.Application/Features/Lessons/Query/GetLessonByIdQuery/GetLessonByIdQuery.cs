using CodeKids.Domain.Abstractions;
using CodeKids.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Lessons;

public sealed record GetLessonByIdQuery(Guid LessonId) : IQuery<LessonDto?>;
