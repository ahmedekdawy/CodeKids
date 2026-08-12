using CodeKids.Application.Abstractions;
using CodeKids.Application.Features.Auth;
using CodeKids.Domain;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Admin;

public sealed record TeacherCourseRateDto(
    Guid CourseId,
    string CourseName,
    int? CourseGrade,
    decimal? SessionAmount,
    decimal? MonthlySalary);
