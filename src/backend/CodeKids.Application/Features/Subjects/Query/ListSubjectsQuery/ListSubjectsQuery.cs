using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;

namespace CodeKids.Application.Features.Subjects;

public sealed record SubjectDto(
    int Id,
    string Title,
    int StageId,
    string Code = "",
    string Category = "",
    string NameEn = "");

public sealed record ListSubjectsQuery(int? StageId = null) : IQuery<IReadOnlyList<SubjectDto>>;
