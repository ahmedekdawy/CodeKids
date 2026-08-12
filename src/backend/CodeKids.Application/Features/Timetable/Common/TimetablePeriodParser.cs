using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Timetable;

public static class TimetablePeriodParser
{
    public static TimetablePeriod Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Period must be am or pm.");
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "am" => TimetablePeriod.Am,
            "pm" => TimetablePeriod.Pm,
            _ => throw new InvalidOperationException("Period must be am or pm.")
        };
    }

    public static TimetablePeriod? ParseOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Parse(value);
    }
}
