using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Media;

public sealed record TeacherVideoLibraryDto(
    IReadOnlyList<TeacherLessonVideoDto> LessonVideos,
    IReadOnlyList<TeacherSolutionVideoDto> SolutionVideos);
