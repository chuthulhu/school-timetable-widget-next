using System.Globalization;
using System.Windows.Media;
using SchoolTimetableWidget.Core.Features.CurrentStatus;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;
using SchoolTimetableWidget.Tests.Time;
using SchoolTimetableWidget.Tests.Timetable;

namespace SchoolTimetableWidget.Tests.DisplaySettings;

public class DisplayModelTests
{
    [Theory]
    [InlineData(DisplayPreset.Standard, DisplayLayout.Standard, 16, true, true, true)]
    [InlineData(DisplayPreset.Digital, DisplayLayout.Digital, 48, true, true, true)]
    [InlineData(DisplayPreset.Compact, DisplayLayout.Inline, 16, false, false, true)]
    [InlineData(DisplayPreset.Minimal, DisplayLayout.Inline, 24, false, false, false)]
    public void DefaultsAreIndependentEditableValues(DisplayPreset preset, DisplayLayout layout, double size, bool seconds, bool date, bool status)
    {
        var value = DisplayPresets.Create(preset);
        value.Validate();
        Assert.Equal(preset, value.Preset); Assert.Equal(layout, value.Layout);
        Assert.Equal(size, value.Time.Size); Assert.Equal(seconds, value.ShowSeconds);
        Assert.Equal(date, value.ShowDate); Assert.Equal(status, value.ShowStatus);
        Assert.True(value.Use24Hour); Assert.False(value.ShowWeekday);
        var changed = value with { Time = value.Time with { Font = new(FontSourceKind.System, "Any Installed Family"), Size = 56 } };
        changed.Validate();
        Assert.Equal(value.Layout, changed.Layout);
        Assert.Equal(value.Date, changed.Date); Assert.Equal(value.Weekday, changed.Weekday); Assert.Equal(value.Status, changed.Status);
        Assert.Equal(value, DisplayPresets.Create(preset));
    }

    [Theory]
    [InlineData(0, true, true, "00:23:18", "")]
    [InlineData(0, false, true, "12:23:18", "오전")]
    [InlineData(1, false, false, "1:23", "오전")]
    [InlineData(11, false, true, "11:23:18", "오전")]
    [InlineData(12, false, true, "12:23:18", "오후")]
    [InlineData(13, false, false, "1:23", "오후")]
    [InlineData(23, false, true, "11:23:18", "오후")]
    [InlineData(13, true, false, "13:23", "")]
    public void FormatsUseSameCapturedSnapshot(int hour, bool use24, bool seconds, string time, string amPm)
    {
        var model = new CurrentStatusHeaderViewModel();
        model.SetDisplay(DisplayPresets.Create(DisplayPreset.Standard) with { Use24Hour = use24, ShowSeconds = seconds });
        model.Apply(Text(hour));
        Assert.Equal(time, model.CurrentTimeText); Assert.Equal(amPm, model.AmPmText);
        Assert.Equal("2026년 09월 11일", model.CurrentDateText); Assert.Equal("금요일", model.WeekdayText);
    }

    [Fact]
    public void DisplayPreviewReadsNoAdditionalClockAndDoesNotChangeTimetableFacts() => HighlightTestDispatcher.Run(() =>
    {
        var clock = new FakeApplicationClock(Snapshot(9));
        var header = new CurrentStatusHeaderViewModel();
        var table = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        using var loop = new CurrentStatusRefreshLoop(clock, DefaultPeriodSchedule.Periods, header, table);
        loop.RefreshNow();
        var cells = table.Cells.ToArray();
        var columns = table.Columns.ToArray();
        var current = table.Cells.Single(c => c.IsCurrent);
        var owner = new RuntimeDisplaySettings(header.Display, _ => throw new Exception("Preview must not save."));
        owner.Changed += (_, _) => header.SetDisplay(owner.Current);
        var draft = owner.Open();
        draft.Preset = DisplayPreset.Digital;
        draft.Use24Hour = false; draft.ShowSeconds = false; draft.ShowDate = false; draft.ShowWeekday = true; draft.ShowStatus = false;
        Assert.Equal("9:23", header.CurrentTimeText); Assert.Equal("오전", header.AmPmText);
        Assert.Equal(1, clock.ReadCount);
        Assert.Equal(cells, table.Cells); Assert.Equal(columns, table.Columns); Assert.True(current.IsCurrent);
        draft.Cancel();
        Assert.Equal("09:23:18", header.CurrentTimeText); Assert.Equal(1, clock.ReadCount);
    });

    [Theory]
    [InlineData("0")] [InlineData("-1")] [InlineData("97")] [InlineData("NaN")] [InlineData("Infinity")] [InlineData("")] [InlineData("한글")]
    public void InvalidTimeSizeRetainsPreviewAndBlocksApply(string text)
    {
        var owner = Owner();
        var session = owner.Open();
        session.Elements[0].SizeText = text;
        Assert.NotEmpty(session.ErrorText); Assert.False(session.TryApply());
        Assert.Equal(owner.Committed, owner.Current);
        session.Elements[0].SizeText = "56";
        Assert.Empty(session.ErrorText); Assert.Equal(56, owner.Current.Time.Size);
        session.Cancel();
    }

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void SupportingElementsValidateTheirOwnRange(int index)
    {
        var owner = Owner(); var session = owner.Open();
        session.Elements[index].SizeText = "49"; Assert.False(session.TryApply());
        session.Elements[index].SizeText = "8"; Assert.True(session.TryApply());
    }

    [Fact]
    public void MissingFontFallsBackWithoutReplacingSavedIdentity() => HighlightTestDispatcher.Run(() =>
    {
        var catalog = new SystemFontCatalog([new FontFamily("Segoe UI")]);
        var selection = new FontSelection(FontSourceKind.System, "Missing Family 7f995");
        Assert.False(catalog.IsAvailable(selection.Family));
        Assert.Equal("Segoe UI", catalog.Resolve(selection).Source);
        Assert.Equal("Missing Family 7f995", selection.Family);
        Assert.NotEmpty(SystemFontCatalog.Current.Families);
    });

    [Theory]
    [InlineData("C:\\Fonts\\x.ttf")] [InlineData("https://example.com/font")] [InlineData("#Some Font")]
    public void FontIdentityCannotBecomeAPathOrRemoteResource(string family) =>
        Assert.Throws<ArgumentException>(() => (DisplayPresets.Create(DisplayPreset.Standard) with
        { Time = DisplayPresets.Create(DisplayPreset.Standard).Time with { Font = new(FontSourceKind.System, family) } }).Validate());

    internal static RuntimeDisplaySettings Owner() => new(DisplayPresets.Create(DisplayPreset.Standard), _ => null);
    internal static ApplicationTimeSnapshot Snapshot(int hour) => new(new DateTimeOffset(2026, 9, 11, hour, 23, 18,
        TimeSpan.FromHours(9)), ApplicationTimeSource.PcLocalFallback, 0);
    internal static CurrentStatusHeaderText Text(int hour)
    {
        var snapshot = Snapshot(hour);
        var status = CurrentStatusResolver.Resolve(snapshot, DefaultPeriodSchedule.Periods);
        return CurrentStatusHeaderFormatter.Format(snapshot, status, CurrentStatusCountdownCalculator.Calculate(snapshot, status));
    }
}
