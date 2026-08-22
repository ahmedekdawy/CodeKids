using System.Text;
using System.Text.Json;
using CodeKids.Application.Abstractions;
using CodeKids.Domain.Abstractions;
using CodeKids.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeKids.Application.Features.StudyPlans;

public sealed class GenerateWeeklyStudyPlanCommandHandler(
    IAppDbContext dbContext,
    IStudyPlanAiClient aiClient)
    : ICommandHandler<GenerateWeeklyStudyPlanCommand, GenerateWeeklyStudyPlanResult>
{
    public async Task<GenerateWeeklyStudyPlanResult> Handle(
        GenerateWeeklyStudyPlanCommand command,
        CancellationToken cancellationToken)
    {
        StudyPlanAccess.ValidateRange(command.FromDate, command.ToDate);
        await StudyPlanAccess.EnsureTeacherOwnsCourseAsync(
            dbContext, command.TeacherId, command.CourseId, cancellationToken);

        var course = await dbContext.Courses
            .AsNoTracking()
            .Include(x => x.Units)
                .ThenInclude(x => x.Lessons)
            .Include(x => x.Lessons)
            .FirstOrDefaultAsync(x => x.Id == command.CourseId, cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");

        var weeks = StudyPlanAccess.BuildSchoolWeeks(command.FromDate, command.ToDate);
        var arabic = IsArabic(command.Language);
        DraftPlan? draft = null;
        try
        {
            var json = await aiClient.CompleteJsonAsync(
                BuildSystemPrompt(arabic),
                BuildUserPrompt(course, weeks.Count, arabic),
                cancellationToken);
            draft = ParseDraft(json);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch
        {
            draft = null;
        }

        if (draft?.Weeks is null || draft.Weeks.Count == 0)
        {
            draft = BuildFallbackDraft(course, weeks.Count, arabic);
        }
        return MapToResult(weeks, draft, arabic);
    }

    private static bool IsArabic(string? language)
    {
        var value = (language ?? string.Empty).Trim().ToLowerInvariant();
        return value.StartsWith("ar") || value.Contains("arab", StringComparison.Ordinal);
    }

    private static string BuildSystemPrompt(bool arabic) =>
        arabic
            ? """
              أنت منسق مناهج مدرسية. وزّع منهج المادة على الأسابيع الدراسية.
              أرجع JSON فقط بهذا الشكل:
              {"notes":"ملاحظة قصيرة","weeks":[{"weekNumber":1,"topics":[{"title":"موضوع","highlight":false}]}]}
              قواعد: weekNumber يبدأ من 1، موضوع واحد فقط لكل أسبوع في title، والنص يمكن أن يكون فقرة قصيرة في خانة واحدة، highlight للمراجعة أو الاختبار فقط.
              """
            : """
              You are a school curriculum planner. Distribute the course across school weeks.
              Return JSON only in this shape:
              {"notes":"short note","weeks":[{"weekNumber":1,"topics":[{"title":"topic","highlight":false}]}]}
              Rules: weekNumber starts at 1, exactly one topic per week in title (a short paragraph is allowed), highlight only a review or quiz week.
              """;

    private static string BuildUserPrompt(Course course, int weekCount, bool arabic)
    {
        var sb = new StringBuilder();
        if (arabic)
        {
            sb.AppendLine($"المادة: {course.Title}");
            if (!string.IsNullOrWhiteSpace(course.Description))
            {
                sb.AppendLine($"الوصف: {course.Description}");
            }

            sb.AppendLine($"عدد الأسابيع: {weekCount}");
            sb.AppendLine("المحتوى:");
        }
        else
        {
            sb.AppendLine($"Course: {course.Title}");
            if (!string.IsNullOrWhiteSpace(course.Description))
            {
                sb.AppendLine($"Description: {course.Description}");
            }

            sb.AppendLine($"Week count: {weekCount}");
            sb.AppendLine("Curriculum:");
        }

        var units = course.Units
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title)
            .ToList();
        if (units.Count > 0)
        {
            foreach (var unit in units)
            {
                sb.AppendLine($"- {unit.Title}");
                foreach (var lesson in unit.Lessons.OrderBy(x => x.SortOrder).ThenBy(x => x.Title).Take(12))
                {
                    sb.AppendLine($"  - {lesson.Title}");
                }
            }
        }
        else
        {
            foreach (var lesson in course.Lessons.OrderBy(x => x.SortOrder).ThenBy(x => x.Title).Take(60))
            {
                sb.AppendLine($"- {lesson.Title}");
            }
        }

        if (arabic)
        {
            sb.AppendLine($"أنشئ خطة كاملة لـ {weekCount} أسبوعاً.");
        }
        else
        {
            sb.AppendLine($"Create a complete plan for {weekCount} weeks.");
        }

        return sb.ToString();
    }

    private static DraftPlan? ParseDraft(string json)
    {
        var payload = ExtractJson(json);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var notes = ReadString(root, "notes", "Notes");
            if (!TryGetProperty(root, out var weeksEl, "weeks", "Weeks")
                || weeksEl.ValueKind != JsonValueKind.Array)
            {
                return string.IsNullOrWhiteSpace(notes) ? null : new DraftPlan(notes, []);
            }

            var weeks = new List<DraftWeek>();
            foreach (var weekEl in weeksEl.EnumerateArray())
            {
                if (weekEl.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var weekNumber = ReadInt(weekEl, "weekNumber", "WeekNumber", "week", "Week");
                if (weekNumber <= 0)
                {
                    weekNumber = weeks.Count + 1;
                }

                weeks.Add(new DraftWeek(weekNumber, ReadTopics(weekEl)));
            }

            return weeks.Count == 0 && string.IsNullOrWhiteSpace(notes)
                ? null
                : new DraftPlan(notes, weeks);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string ExtractJson(string raw)
    {
        var text = (raw ?? string.Empty).Trim();
        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf("```", StringComparison.Ordinal);
            if (start >= 0 && end > start)
            {
                text = text[start..end].Trim();
            }
        }

        var from = text.IndexOf('{');
        var to = text.LastIndexOf('}');
        return from >= 0 && to > from ? text[from..(to + 1)] : text;
    }

    private static DraftPlan BuildFallbackDraft(Course course, int weekCount, bool arabic)
    {
        var topics = new List<(string Title, bool Highlight)>();
        foreach (var unit in course.Units.OrderBy(x => x.SortOrder).ThenBy(x => x.Title))
        {
            if (!string.IsNullOrWhiteSpace(unit.Title))
            {
                topics.Add((unit.Title.Trim(), true));
            }

            foreach (var lesson in unit.Lessons.OrderBy(x => x.SortOrder).ThenBy(x => x.Title))
            {
                if (!string.IsNullOrWhiteSpace(lesson.Title))
                {
                    topics.Add((lesson.Title.Trim(), false));
                }
            }
        }

        if (topics.Count == 0)
        {
            foreach (var lesson in course.Lessons.OrderBy(x => x.SortOrder).ThenBy(x => x.Title))
            {
                if (!string.IsNullOrWhiteSpace(lesson.Title))
                {
                    topics.Add((lesson.Title.Trim(), false));
                }
            }
        }

        if (topics.Count == 0)
        {
            var review = arabic ? "مراجعة ومناقشة" : "Review and discussion";
            for (var i = 1; i <= weekCount; i++)
            {
                topics.Add(($"{course.Title} — {review} {i}", i == weekCount));
            }
        }

        var perWeek = Math.Max(1, (int)Math.Ceiling(topics.Count / (double)Math.Max(weekCount, 1)));
        var weeks = new List<DraftWeek>();
        var index = 0;
        for (var week = 1; week <= weekCount; week++)
        {
            var slice = new List<string>();
            var highlight = false;
            for (var i = 0; i < perWeek && index < topics.Count; i++, index++)
            {
                slice.Add(topics[index].Title);
                highlight = highlight || topics[index].Highlight;
            }

            if (slice.Count == 0 && topics.Count > 0)
            {
                var wrap = topics[(week - 1) % topics.Count];
                slice.Add(wrap.Title);
                highlight = wrap.Highlight;
            }

            weeks.Add(new DraftWeek(week, [new DraftTopic(string.Join(" — ", slice), highlight)]));
        }

        var notes = arabic
            ? "خطة مقترحة من المنهج. راجعها قبل الحفظ."
            : "Draft plan from the course curriculum. Review before saving.";
        return new DraftPlan(notes, weeks);
    }

    private static GenerateWeeklyStudyPlanResult MapToResult(
        List<(int WeekNumber, DateOnly FromDate, DateOnly ToDate)> weeks,
        DraftPlan draft,
        bool arabic)
    {
        var byNumber = (draft.Weeks ?? [])
            .Where(x => x.WeekNumber > 0)
            .GroupBy(x => x.WeekNumber)
            .ToDictionary(g => g.Key, g => g.Last());

        var mapped = weeks.Select(week =>
        {
            byNumber.TryGetValue(week.WeekNumber, out var match);
            var topic = CombineTopic(match?.Topics, arabic ? "موضوع الأسبوع" : "Week topic");
            return new SaveWeeklyStudyPlanWeekDto(week.WeekNumber, week.FromDate, week.ToDate, [topic]);
        }).ToList();

        return new GenerateWeeklyStudyPlanResult(
            StudyPlanAccess.Clamp(draft.Notes, 1000),
            mapped);
    }

    private static SaveWeeklyStudyPlanTopicDto CombineTopic(IEnumerable<DraftTopic>? topics, string fallback)
    {
        var list = (topics ?? []).ToList();
        var titles = list
            .Select(topic => StudyPlanAccess.Clamp(topic.Title, StudyPlanAccess.TopicTitleMax).Trim())
            .Where(title => title.Length > 0)
            .ToList();
        var title = titles.Count == 0
            ? fallback
            : StudyPlanAccess.Clamp(string.Join("\n", titles), StudyPlanAccess.TopicTitleMax);
        return new SaveWeeklyStudyPlanTopicDto(title, list.Any(topic => topic.Highlight));
    }

    private static List<DraftTopic> ReadTopics(JsonElement weekEl)
    {
        if (TryGetProperty(weekEl, out var topicsEl, "topics", "Topics"))
        {
            return ReadTopicList(topicsEl);
        }

        var title = ReadString(weekEl, "title", "Title", "topic", "Topic", "content", "Content");
        return string.IsNullOrWhiteSpace(title)
            ? []
            : [new DraftTopic(title, ReadBool(weekEl, "highlight", "Highlight"))];
    }

    private static List<DraftTopic> ReadTopicList(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.String)
        {
            var title = el.GetString();
            return string.IsNullOrWhiteSpace(title) ? [] : [new DraftTopic(title, false)];
        }

        if (el.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var list = new List<DraftTopic>();
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var title = item.GetString();
                if (!string.IsNullOrWhiteSpace(title))
                {
                    list.Add(new DraftTopic(title, false));
                }

                continue;
            }

            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var text = ReadString(item, "title", "Title", "topic", "Topic", "content", "Content", "text", "Text");
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            list.Add(new DraftTopic(text, ReadBool(item, "highlight", "Highlight")));
        }

        return list;
    }

    private static bool TryGetProperty(JsonElement el, out JsonElement value, params string[] names)
    {
        foreach (var name in names)
        {
            if (el.TryGetProperty(name, out value))
            {
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string? ReadString(JsonElement el, params string[] names)
    {
        return TryGetProperty(el, out var value, names) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static int ReadInt(JsonElement el, params string[] names)
    {
        if (!TryGetProperty(el, out var value, names))
        {
            return 0;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), out var parsed) => parsed,
            _ => 0
        };
    }

    private static bool ReadBool(JsonElement el, params string[] names)
    {
        if (!TryGetProperty(el, out var value, names))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(value.GetString(), out var parsed) && parsed,
            _ => false
        };
    }

    private sealed record DraftPlan(string? Notes, IReadOnlyList<DraftWeek>? Weeks);
    private sealed record DraftWeek(int WeekNumber, IReadOnlyList<DraftTopic>? Topics);
    private sealed record DraftTopic(string? Title, bool Highlight);
}
