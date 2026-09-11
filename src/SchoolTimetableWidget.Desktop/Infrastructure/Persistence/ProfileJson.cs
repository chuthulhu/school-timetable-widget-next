using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Desktop.Features.Persistence;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Persistence;

/// <summary>Versioned storage DTOs. Domain values have no serialization attributes.</summary>
public static class ProfileJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        AllowDuplicateProperties = false
    };

    public static byte[] Serialize(ProfileSnapshot value)
    {
        var document = new Document(2, new Profile(
            value.Timetable.Cells.Select(Cell.From).ToArray(), Period.From(value.Schedule),
            value.Overrides.Select(e => new Override(
                e.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                e.Timetable is null ? null : e.Timetable.Values.Select((v, i) => new Cell(e.Day.ToString(), i + 1, v.SubjectText, v.ClassText)).ToArray(),
                e.Schedule is null ? null : Period.From(e.Schedule))).ToArray(), new Presentation(value.ShowLunch), Display.From(value.Display)));
        var bytes = JsonSerializer.SerializeToUtf8Bytes(document, Options);
        // Validate the complete storage document before any file I/O.
        _ = Deserialize(bytes);
        return bytes;
    }

    public static ProfileSnapshot Deserialize(ReadOnlySpan<byte> bytes)
    {
        using var json = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { AllowDuplicateProperties = false });
        if (!json.RootElement.TryGetProperty("schemaVersion", out var version) || !version.TryGetInt32(out var number))
            throw new JsonException("Missing schema version.");
        if (number is not (1 or 2)) throw new UnsupportedProfileVersionException();
        var document = number == 1
            ? Upgrade(JsonSerializer.Deserialize<DocumentV1>(bytes, Options) ?? throw new JsonException("Null document."))
            : JsonSerializer.Deserialize<Document>(bytes, Options) ?? throw new JsonException("Null document.");
        var profile = document.Profile;
        if (profile.Timetable.Any(c => c is null) || profile.DateOverrides.Any(e => e is null))
            throw new JsonException("Null array entry.");
        var week = new WeeklyTimetable(profile.Timetable.Select(c => c.ToDomain()));
        var schedule = Period.ToDomain(profile.PeriodSchedule);
        var overrides = profile.DateOverrides.Select(entry =>
        {
            var date = DateOnly.ParseExact(entry.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            DayTimetable? day = null;
            if (entry.Timetable is { } cells)
            {
                if (cells.Length != 7 || cells.Any(c => c is null)) throw new JsonException("Incomplete date timetable.");
                var expectedDay = (SchoolDay)((int)date.DayOfWeek - 1);
                var domain = cells.Select(c => c.ToDomain()).ToArray();
                if (domain.Where((c, i) => c.Day != expectedDay || c.PeriodNumber != i + 1).Any())
                    throw new JsonException("Date timetable identity mismatch.");
                day = new(domain.Select(c => c.Value));
            }
            return new DateSpecificOverride(date, day, entry.PeriodSchedule is null ? null : Period.ToDomain(entry.PeriodSchedule));
        });
        return new(week, schedule, overrides, profile.Presentation.ShowLunchBetweenPeriods4And5, profile.Display.ToValue());
    }

    private sealed record Document(int SchemaVersion, Profile Profile);
    private sealed record Profile(Cell[] Timetable, Period[] PeriodSchedule, Override[] DateOverrides, Presentation Presentation, Display Display);
    private sealed record DocumentV1(int SchemaVersion, ProfileV1 Profile);
    private sealed record ProfileV1(Cell[] Timetable, Period[] PeriodSchedule, Override[] DateOverrides, Presentation Presentation);
    private static Document Upgrade(DocumentV1 old) => new(2, new(old.Profile.Timetable, old.Profile.PeriodSchedule,
        old.Profile.DateOverrides, old.Profile.Presentation, Display.From(DisplayPresets.Create(DisplayPreset.Standard))));

    private static T ParseName<T>(string name) where T : struct, Enum =>
        Enum.TryParse<T>(name, out var value) && Enum.IsDefined(value) && value.ToString() == name
            ? value : throw new JsonException("Invalid display value.");

    private sealed record Font(string Source, string Family);
    private sealed record Typography(Font Font, double Size, string Weight, string Style)
    {
        public static Typography From(ElementTypography value) => new(new(value.Font.Source.ToString(), value.Font.Family),
            value.Size, value.Weight.ToString(), value.Style.ToString());
        public ElementTypography ToValue() => new(new(ParseName<FontSourceKind>(Font.Source), Font.Family),
            Size, ParseName<DisplayFontWeight>(Weight), ParseName<DisplayFontStyle>(Style));
    }
    private sealed record Display(string Preset, string Layout, Typography Time, Typography Date, Typography Weekday,
        Typography Status, bool Use24Hour, bool ShowSeconds, bool ShowDate, bool ShowWeekday, bool ShowStatus)
    {
        public static Display From(DisplayConfiguration value) => new(value.Preset.ToString(), value.Layout.ToString(),
            Typography.From(value.Time), Typography.From(value.Date), Typography.From(value.Weekday), Typography.From(value.Status),
            value.Use24Hour, value.ShowSeconds, value.ShowDate, value.ShowWeekday, value.ShowStatus);
        public DisplayConfiguration ToValue()
        {
            var value = new DisplayConfiguration(ParseName<DisplayPreset>(Preset), ParseName<DisplayLayout>(Layout),
                Time.ToValue(), Date.ToValue(), Weekday.ToValue(), Status.ToValue(),
                Use24Hour, ShowSeconds, ShowDate, ShowWeekday, ShowStatus);
            value.Validate();
            return value;
        }
    }
    private sealed record Presentation(bool ShowLunchBetweenPeriods4And5);
    private sealed record Override(string Date, Cell[]? Timetable, Period[]? PeriodSchedule);
    private sealed record Cell(string SchoolDay, int PeriodNumber, string SubjectText, string ClassText)
    {
        public static Cell From(TimetableCell c) => new(c.Day.ToString(), c.PeriodNumber, c.Value.SubjectText, c.Value.ClassText);
        public TimetableCell ToDomain()
        {
            // Only canonical names; numeric enum strings and locale-dependent names are rejected.
            if (!Enum.TryParse<SchoolDay>(SchoolDay, out var day) || !Enum.IsDefined(day) || day.ToString() != SchoolDay)
                throw new JsonException("Invalid school day.");
            return new(day, PeriodNumber, new(SubjectText, ClassText));
        }
    }
    private sealed record Period(int PeriodNumber, string Start, string End)
    {
        private const string TimeFormat = "HH:mm:ss.fffffff";
        public static Period[] From(PeriodSchedule schedule) => schedule.Periods.Select(p => new Period(p.PeriodNumber,
            p.Start.ToString(TimeFormat, CultureInfo.InvariantCulture), p.End.ToString(TimeFormat, CultureInfo.InvariantCulture))).ToArray();
        public static PeriodSchedule ToDomain(Period[] periods)
        {
            if (periods.Any(p => p is null)) throw new JsonException("Null period.");
            return new(periods.Select(p => new PeriodDefinition(p.PeriodNumber,
                TimeOnly.ParseExact(p.Start, TimeFormat, CultureInfo.InvariantCulture), TimeOnly.ParseExact(p.End, TimeFormat, CultureInfo.InvariantCulture))));
        }
    }
}

public sealed class UnsupportedProfileVersionException : Exception;
