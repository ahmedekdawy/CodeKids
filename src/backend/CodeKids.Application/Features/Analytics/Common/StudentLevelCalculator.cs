using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using CodeKids.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.Analytics;

public static class StudentLevelCalculator
{
    private static readonly (int MinXp, string Code, string Name)[] Levels =
    [
        (0, "L1", "Beginner"),
        (100, "L2", "Explorer"),
        (250, "L3", "Coder"),
        (500, "L4", "Pro"),
        (1000, "L5", "Master")
    ];

    public static (int LevelNumber, string Code, string Name, int MinXp, int? NextMinXp, int ProgressPercent) FromXp(int totalXp)
    {
        var xp = Math.Max(0, totalXp);
        var index = 0;
        for (var i = 0; i < Levels.Length; i++)
        {
            if (xp >= Levels[i].MinXp) index = i;
        }

        var current = Levels[index];
        int? nextMin = index + 1 < Levels.Length ? Levels[index + 1].MinXp : null;
        var progress = nextMin is int next
            ? (int)Math.Clamp(Math.Round((xp - current.MinXp) * 100.0 / Math.Max(1, next - current.MinXp)), 0, 100)
            : 100;

        return (index + 1, current.Code, current.Name, current.MinXp, nextMin, progress);
    }
}
