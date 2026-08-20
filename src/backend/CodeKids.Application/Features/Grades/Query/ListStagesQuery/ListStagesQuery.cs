using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Grades;

public sealed record ListStagesQuery() : IQuery<IReadOnlyList<StageDto>>;
