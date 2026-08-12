using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Analytics;

public sealed record ClassroomDiagnosisDto(
    Guid ClassroomId,
    string ClassroomName,
    IReadOnlyList<LessonWeaknessDto> WeakLessons,
    IReadOnlyList<string> BehindStudents,
    IReadOnlyList<string> StrongStudents);
