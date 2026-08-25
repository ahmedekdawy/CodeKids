using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Auth;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Admin;

public sealed record CourseSummaryDto(
    Guid Id,
    string Title,
    string Theme,
    string Description,
    int AgeMin,
    int AgeMax,
    string? Term,
    int? Grade,
    int? StageId,
    int SortOrder,
    string? SchoolType = null,
    int? ExternalSubjectId = null,
    string SubjectCode = "",
    string Category = "",
    string TrackCode = "",
    string TrackName = "",
    string VerificationStatus = "");
