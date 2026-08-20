namespace CodeKids.Application.Features.Grades;

public sealed record StageDto(int Id, string Name, string NameEn);

public sealed record GradeDto(int Id, string Name, string NameEn, int StageId);
