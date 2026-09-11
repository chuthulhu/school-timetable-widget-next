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
        var document = new Document(4, new Profile(
            value.Timetable.Cells.Select(Cell.From).ToArray(), Period.From(value.Schedule),
            value.Overrides.Select(e => new Override(
                e.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                e.Timetable is null ? null : e.Timetable.Values.Select((v, i) => new Cell(e.Day.ToString(), i + 1, v.SubjectText, v.ClassText)).ToArray(),
                e.Schedule is null ? null : Period.From(e.Schedule))).ToArray(), new Presentation(value.ShowLunch), DisplayV3.From(value.Display),
                value.DisplayPresets.Items.Select(UserPreset.From).ToArray()));
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
        if (number is not (1 or 2 or 3 or 4)) throw new UnsupportedProfileVersionException();
        var document = number switch
        {
            1 => Upgrade(JsonSerializer.Deserialize<DocumentV1>(bytes, Options) ?? throw new JsonException("Null document.")),
            2 => Upgrade(JsonSerializer.Deserialize<DocumentV2>(bytes, Options) ?? throw new JsonException("Null document.")),
            3 => Upgrade(JsonSerializer.Deserialize<DocumentV3>(bytes, Options) ?? throw new JsonException("Null document.")),
            _ => JsonSerializer.Deserialize<Document>(bytes, Options) ?? throw new JsonException("Null document.")
        };
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
        return new(week, schedule, overrides, profile.Presentation.ShowLunchBetweenPeriods4And5, profile.Display.ToValue(),
            new UserDisplayPresetLibrary(profile.DisplayPresets.Select(p => p is null
                ? throw new JsonException("Null user preset.") : p.ToValue())));
    }

    private sealed record Document(int SchemaVersion, Profile Profile);
    private sealed record Profile(Cell[] Timetable, Period[] PeriodSchedule, Override[] DateOverrides,
        Presentation Presentation, DisplayV3 Display, UserPreset[] DisplayPresets);
    private sealed record DocumentV2(int SchemaVersion, ProfileV2 Profile);
    private sealed record ProfileV2(Cell[] Timetable, Period[] PeriodSchedule, Override[] DateOverrides, Presentation Presentation, Display Display);
    private sealed record DocumentV1(int SchemaVersion, ProfileV1 Profile);
    private sealed record ProfileV1(Cell[] Timetable, Period[] PeriodSchedule, Override[] DateOverrides, Presentation Presentation);
    private static Document Upgrade(DocumentV1 old) => new(4, new(old.Profile.Timetable, old.Profile.PeriodSchedule,
        old.Profile.DateOverrides, old.Profile.Presentation, DisplayV3.From(DisplayPresets.Create(DisplayPreset.Standard)), []));
    private static Document Upgrade(DocumentV2 old) => new(4, new(old.Profile.Timetable, old.Profile.PeriodSchedule,
        old.Profile.DateOverrides, old.Profile.Presentation, DisplayV3.From(old.Profile.Display.ToValue()), []));

    private static T ParseName<T>(string name) where T : struct, Enum =>
        Enum.TryParse<T>(name, out var value) && Enum.IsDefined(value) && value.ToString() == name
            ? value : throw new JsonException("Invalid display value.");

    private sealed record Font(string Source, string Family, string FamilyId);
    private sealed record LegacyFont(string Source, string Family);
    private sealed record LegacyTypography(LegacyFont Font, double Size, string Weight, string Style)
    {
        public ElementTypography ToValue() => Font.Source == "System"
            ? new(new(FontSourceKind.System, Font.Family), Size, ParseName<DisplayFontWeight>(Weight), ParseName<DisplayFontStyle>(Style))
            : throw new JsonException("Unsupported legacy font source.");
    }
    private sealed record Typography(Font Font, double Size, string Weight, string Style)
    {
        public static Typography From(ElementTypography value) => new(new(value.Font.Source.ToString(), value.Font.Family, value.Font.FamilyId),
            value.Size, value.Weight.ToString(), value.Style.ToString());
        public ElementTypography ToValue() => new(new(ParseName<FontSourceKind>(Font.Source), Font.Family, Font.FamilyId),
            Size, ParseName<DisplayFontWeight>(Weight), ParseName<DisplayFontStyle>(Style));
    }
    private sealed record Display(string Preset, string Layout, LegacyTypography Time, LegacyTypography Date, LegacyTypography Weekday,
        LegacyTypography Status, bool Use24Hour, bool ShowSeconds, bool ShowDate, bool ShowWeekday, bool ShowStatus)
    {
        public DisplayConfiguration ToValue()
        {
            var value = new DisplayConfiguration(ParseName<DisplayPreset>(Preset), ParseName<DisplayLayout>(Layout),
                Time.ToValue(), Date.ToValue(), Weekday.ToValue(), Status.ToValue(),
                Use24Hour, ShowSeconds, ShowDate, ShowWeekday, ShowStatus);
            value.Validate();
            return value;
        }
    }
    private sealed record PresetReference(string Kind, string? BuiltIn, string? UserId)
    {
        public static PresetReference From(DisplayPresetReference value) => value.BuiltIn is { } builtIn
            ? new("BuiltIn", builtIn.ToString(), null) : new("User", null, value.UserId!.Value.ToString("D"));
        public DisplayPresetReference ToValue() => (Kind, BuiltIn, UserId) switch
        {
            ("BuiltIn", { } name, null) => DisplayPresetReference.BuiltInPreset(ParseName<DisplayPreset>(name)),
            ("User", null, { } id) => DisplayPresetReference.User(ParseId(id)),
            _ => throw new JsonException("Invalid preset reference.")
        };
    }
    private static Guid ParseId(string id) => Guid.TryParseExact(id, "D", out var value) && value != Guid.Empty
        ? value : throw new JsonException("Invalid preset ID.");
    private sealed record DocumentV3(int SchemaVersion, ProfileV3 Profile);
    private sealed record ProfileV3(Cell[] Timetable, Period[] PeriodSchedule, Override[] DateOverrides,
        Presentation Presentation, LegacyDisplay Display, LegacyUserPreset[] DisplayPresets);
    private static Document Upgrade(DocumentV3 old) => new(4, new(old.Profile.Timetable, old.Profile.PeriodSchedule,
        old.Profile.DateOverrides, old.Profile.Presentation, DisplayV3.From(old.Profile.Display.ToValue()),
        old.Profile.DisplayPresets.Select(p => p is null ? throw new JsonException("Null user preset.") : UserPreset.From(p.ToValue())).ToArray()));
    private sealed record LegacyDisplay(PresetReference Preset, LegacyPayload Settings)
    {
        public DisplayConfiguration ToValue() => Settings.ToValue(Preset.ToValue());
    }
    private sealed record LegacyUserPreset(string Id, string Name, LegacyPayload Settings)
    {
        public UserDisplayPreset ToValue()
        {
            var id = ParseId(Id);
            return new(id, Name, Settings.ToValue(DisplayPresetReference.User(id)));
        }
    }
    private sealed record LegacyPayload(string Layout, LegacyTypography Time, LegacyTypography Date, LegacyTypography Weekday,
        LegacyTypography Status, bool Use24Hour, bool ShowSeconds, bool ShowDate, bool ShowWeekday, bool ShowStatus)
    {
        public DisplayConfiguration ToValue(DisplayPresetReference reference)
        {
            var value = new DisplayConfiguration(reference, ParseName<DisplayLayout>(Layout),
                Time.ToValue(), Date.ToValue(), Weekday.ToValue(), Status.ToValue(),
                Use24Hour, ShowSeconds, ShowDate, ShowWeekday, ShowStatus);
            value.Validate();
            return value;
        }
    }
    private sealed record DisplayV3(PresetReference Preset, DisplayPayload Settings)
    {
        public static DisplayV3 From(DisplayConfiguration value) => new(PresetReference.From(value.Preset), DisplayPayload.From(value));
        public DisplayConfiguration ToValue() => Settings.ToValue(Preset.ToValue());
    }
    private sealed record UserPreset(string Id, string Name, DisplayPayload Settings)
    {
        public static UserPreset From(UserDisplayPreset value) => new(value.Id.ToString("D"), value.Name, DisplayPayload.From(value.Display));
        public UserDisplayPreset ToValue()
        {
            var id = ParseId(Id);
            return new(id, Name, Settings.ToValue(DisplayPresetReference.User(id)));
        }
    }
    private sealed record DisplayPayload(string Layout, Typography Time, Typography Date, Typography Weekday,
        Typography Status, bool Use24Hour, bool ShowSeconds, bool ShowDate, bool ShowWeekday, bool ShowStatus)
    {
        public static DisplayPayload From(DisplayConfiguration value) => new(value.Layout.ToString(),
            Typography.From(value.Time), Typography.From(value.Date), Typography.From(value.Weekday), Typography.From(value.Status),
            value.Use24Hour, value.ShowSeconds, value.ShowDate, value.ShowWeekday, value.ShowStatus);
        public DisplayConfiguration ToValue(DisplayPresetReference reference)
        {
            var value = new DisplayConfiguration(reference, ParseName<DisplayLayout>(Layout),
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
