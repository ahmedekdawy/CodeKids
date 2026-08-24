using System.Text.Json;

namespace CodeKids.Infrastructure;

internal sealed record CurriculumUnitSpec(
    int Term,
    string Title,
    string VerificationStatus,
    IReadOnlyList<string> Lessons);

internal sealed record CurriculumCourseSpec(
    int Grade,
    int StageId,
    string SubjectCode,
    string Title,
    string NameEn,
    string Category,
    string TrackCode,
    string TrackName,
    string VerificationStatus,
    string Notes,
    string SourceTocUrl,
    string Variants,
    IReadOnlyList<CurriculumUnitSpec> Units);

internal static class EgyptianCurriculumCatalog
{
    public const string ResourceName = "CodeKids.Infrastructure.egypt_curriculum_grades_1_to_12.json";

    public static IReadOnlyList<CurriculumCourseSpec> Load() => LoadCatalog();

    private static IReadOnlyList<CurriculumCourseSpec> LoadCatalog()
    {
        using var stream = typeof(EgyptianCurriculumCatalog).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded curriculum file '{ResourceName}' was not found.");
        using var document = JsonDocument.Parse(stream);

        var offerings = new List<RawOffering>();
        if (!document.RootElement.TryGetProperty("stages", out var stages) || stages.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        foreach (var stage in stages.EnumerateArray())
        {
            var stageId = StageIdFor(Str(stage, "stage_code"));
            if (!stage.TryGetProperty("grades", out var grades) || grades.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var grade in grades.EnumerateArray())
            {
                var gradeNumber = Int(grade, "grade_number");
                if (gradeNumber is < 1 or > 12)
                {
                    continue;
                }

                if (grade.TryGetProperty("terms", out var terms) && terms.ValueKind == JsonValueKind.Array)
                {
                    ReadTerms(terms, gradeNumber, stageId, "", "", offerings);
                }

                if (grade.TryGetProperty("tracks", out var tracks) && tracks.ValueKind == JsonValueKind.Array)
                {
                    foreach (var track in tracks.EnumerateArray())
                    {
                        if (!track.TryGetProperty("terms", out var trackTerms) || trackTerms.ValueKind != JsonValueKind.Array)
                        {
                            continue;
                        }

                        ReadTerms(
                            trackTerms,
                            gradeNumber,
                            stageId,
                            Str(track, "track_code"),
                            Str(track, "name_ar"),
                            offerings);
                    }
                }
            }
        }

        return Merge(offerings);
    }

    private static void ReadTerms(
        JsonElement terms,
        int grade,
        int stageId,
        string trackCode,
        string trackName,
        List<RawOffering> offerings)
    {
        foreach (var term in terms.EnumerateArray())
        {
            var termNumber = Int(term, "term");
            if (termNumber is not (1 or 2))
            {
                continue;
            }

            if (!term.TryGetProperty("subjects", out var subjects) || subjects.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var subject in subjects.EnumerateArray())
            {
                var code = Str(subject, "subject_code");
                var title = Str(subject, "name_ar");
                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(title))
                {
                    continue;
                }

                offerings.Add(new RawOffering(
                    grade,
                    stageId,
                    termNumber,
                    code.Trim(),
                    title.Trim(),
                    Str(subject, "name_en").Trim(),
                    Str(subject, "category").Trim(),
                    trackCode.Trim(),
                    trackName.Trim(),
                    Str(subject, "verification_status").Trim(),
                    Str(subject, "notes").Trim(),
                    ExtractTocUrl(subject),
                    ExtractVariants(subject),
                    ReadUnits(subject, termNumber)));
            }
        }
    }

    private static IReadOnlyList<CurriculumUnitSpec> ReadUnits(JsonElement subject, int term)
    {
        if (!subject.TryGetProperty("units", out var units) || units.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var list = new List<CurriculumUnitSpec>();
        foreach (var unit in units.EnumerateArray())
        {
            var title = Str(unit, "title").Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var lessons = new List<string>();
            if (unit.TryGetProperty("lessons", out var lessonEl) && lessonEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var lesson in lessonEl.EnumerateArray())
                {
                    var lessonTitle = lesson.ValueKind == JsonValueKind.String
                        ? lesson.GetString()
                        : Str(lesson, "title");
                    if (!string.IsNullOrWhiteSpace(lessonTitle))
                    {
                        lessons.Add(lessonTitle.Trim());
                    }
                }
            }

            list.Add(new CurriculumUnitSpec(
                term,
                title,
                Str(unit, "verification_status").Trim(),
                lessons));
        }

        return list;
    }

    private static IReadOnlyList<CurriculumCourseSpec> Merge(IReadOnlyList<RawOffering> offerings)
    {
        return offerings
            .GroupBy(x => (x.Grade, x.SubjectCode, x.TrackCode), x => x)
            .Select(group =>
            {
                var ordered = group.OrderBy(x => x.Term).ToList();
                var first = ordered[0];
                var units = ordered.SelectMany(x => x.Units).ToList();
                var notes = string.Join(" | ", ordered.Select(x => x.Notes).Where(n => n.Length > 0).Distinct());
                var status = ordered
                    .Select(x => x.VerificationStatus)
                    .FirstOrDefault(s => s.Contains("verified", StringComparison.OrdinalIgnoreCase))
                    ?? first.VerificationStatus;
                var toc = ordered.Select(x => x.SourceTocUrl).FirstOrDefault(u => u.Length > 0) ?? "";
                var variants = ordered.Select(x => x.Variants).FirstOrDefault(v => v.Length > 0) ?? "";
                var nameEn = ordered.Select(x => x.NameEn).FirstOrDefault(n => n.Length > 0) ?? "";
                var category = ordered.Select(x => x.Category).FirstOrDefault(c => c.Length > 0) ?? "core";

                return new CurriculumCourseSpec(
                    first.Grade,
                    first.StageId,
                    first.SubjectCode,
                    first.Title,
                    nameEn,
                    category,
                    first.TrackCode,
                    first.TrackName,
                    status,
                    notes,
                    toc,
                    variants,
                    units);
            })
            .OrderBy(x => x.Grade)
            .ThenBy(x => x.TrackCode)
            .ThenBy(x => x.Title)
            .ToList();
    }

    private static string ExtractTocUrl(JsonElement subject)
    {
        if (!subject.TryGetProperty("source", out var source) || source.ValueKind != JsonValueKind.Object)
        {
            return "";
        }

        var direct = Str(source, "toc_url");
        if (direct.Length > 0)
        {
            return direct;
        }

        if (source.TryGetProperty("parts", out var parts) && parts.ValueKind == JsonValueKind.Array)
        {
            foreach (var part in parts.EnumerateArray())
            {
                var url = Str(part, "toc_url");
                if (url.Length > 0)
                {
                    return url;
                }
            }
        }

        return Str(source, "toc_index");
    }

    private static string ExtractVariants(JsonElement subject)
    {
        if (!subject.TryGetProperty("variants", out var variants) || variants.ValueKind != JsonValueKind.Array)
        {
            return "";
        }

        return string.Join("، ", variants.EnumerateArray()
            .Select(v => v.GetString()?.Trim())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!));
    }

    private static int StageIdFor(string stageCode) => stageCode switch
    {
        "primary" => 1,
        "preparatory" => 2,
        "secondary" => 3,
        _ => 1
    };

    private static string Str(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    private static int Int(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.TryGetInt32(out var number)
            ? number
            : 0;

    private sealed record RawOffering(
        int Grade,
        int StageId,
        int Term,
        string SubjectCode,
        string Title,
        string NameEn,
        string Category,
        string TrackCode,
        string TrackName,
        string VerificationStatus,
        string Notes,
        string SourceTocUrl,
        string Variants,
        IReadOnlyList<CurriculumUnitSpec> Units);
}
