using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Desktop.Features.Timetable;

namespace SchoolTimetableWidget.Tests.Timetable;

public class TimetableEditingTests
{
    public static TheoryData<string, string> Pairs => new()
    {
        { "", "" }, { "과학", "" }, { "", "2-3" }, { "국어", "1-2" },
        { "   ", "\t " }, { "  교과  ", "  반  " },
        { "물리학\n실험 A반!", "\r\n반\r둘째\n\n" },
        { "한글 Ω 🎵 e\u0301", "👩‍🔬 \u200b" },
        { "<b>교과</b>", "&amp; {Binding Secret}" },
        { new string('가', 2000), new string('반', 2000) }
    };

    [Theory]
    [MemberData(nameof(Pairs))]
    public void ApplyChangesBothFieldsOfOnlyTheCapturedSlotAndReopenUsesNewBaseline(string subject, string classText)
    {
        var initial = new TimetableCellValue("반복", "같은 반");
        var week = new WeeklyTimetable(WeeklyTimetable.Empty().Cells.Select(cell =>
            new TimetableCell(cell.Day, cell.PeriodNumber, initial)));
        foreach (var index in Enumerable.Range(0, 35))
        {
            var model = new WeeklyTimetableViewModel(week);
            var presentations = model.Cells.ToArray();
            var session = model.Editor.BeginEdit(model.Cells[index]);
            Assert.Same(initial, session.OriginalValue);
            session.SubjectText = subject;
            session.ClassText = classText;
            Assert.Same(week, model.CommittedTimetable);
            Assert.All(model.Cells, cell => Assert.Equal(initial, cell.Value));
            Assert.True(session.TryApply());
            Assert.True(session.IsApplied);
            Assert.True(session.IsClosed);
            Assert.Null(model.Editor.ActiveSession);
            var target = week.Cells[index];
            var expected = new TimetableCellValue(subject, classText);
            Assert.Equal(expected, model.CommittedTimetable[target.Day, target.PeriodNumber].Value);
            Assert.Equal(expected, model.Cells[index].Value);
            Assert.Equal(presentations, model.Cells);
            Assert.All(week.Cells, cell => Assert.Same(initial, cell.Value));
            for (var other = 0; other < 35; other++)
                if (other != index) Assert.Same(week.Cells[other], model.CommittedTimetable.Cells[other]);
            var reopened = model.Editor.BeginEdit(model.Cells[index]);
            Assert.Equal(expected, reopened.OriginalValue);
            reopened.SubjectText = "취소할 교과";
            reopened.ClassText = "취소할 반";
            reopened.Cancel();
            Assert.Equal(expected, model.Cells[index].Value);
            Assert.False(session.TryApply());
            session.Cancel();
            Assert.Equal(expected, model.Cells[index].Value);
        }
    }

    [Fact]
    public void ValueAndProjectionAreCoherentAtEveryNotification()
    {
        var model = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var session = model.Editor.BeginEdit(model.Cells[0]);
        var expected = new TimetableCellValue("교과", "반");
        var notifications = new List<string?>();
        model.Cells[0].PropertyChanged += (_, args) =>
        {
            notifications.Add(args.PropertyName);
            Assert.Equal(expected, model.CommittedTimetable[SchoolDay.Monday, 1].Value);
            Assert.Equal(expected, model.Cells[0].Value);
            Assert.Equal("교과\n반", model.Cells[0].DisplayText);
            Assert.False(session.TryApply()); // no reentrant duplicate commit
        };
        session.SubjectText = expected.SubjectText;
        session.ClassText = expected.ClassText;
        Assert.True(session.TryApply());
        Assert.Equal(new[] { "Value", "DisplayText" }, notifications);
        var unchanged = model.Editor.BeginEdit(model.Cells[0]);
        var snapshot = model.CommittedTimetable;
        Assert.True(unchanged.TryApply());
        Assert.Same(snapshot, model.CommittedTimetable);
        Assert.Equal(2, notifications.Count);
    }

    [Fact]
    public void ClosedDraftCannotAffectLaterSessionAndOnlyOneSessionCanOpen()
    {
        var model = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var first = model.Editor.BeginEdit(model.Cells[0]);
        first.SubjectText = "cancel subject";
        first.ClassText = "cancel class";
        Assert.Throws<InvalidOperationException>(() => model.Editor.BeginEdit(model.Cells[1]));
        first.Cancel();
        Assert.False(first.IsApplied);
        var next = model.Editor.BeginEdit(model.Cells[34]);
        first.Cancel();
        Assert.False(first.TryApply());
        Assert.Same(next, model.Editor.ActiveSession);
        Assert.All(model.Cells, cell => Assert.Equal(new TimetableCellValue("", ""), cell.Value));
        next.Cancel();
        Assert.Throws<ArgumentException>(() => model.Editor.BeginEdit(new(new("", ""))));
        Assert.Throws<ArgumentNullException>(() => model.Editor.BeginEdit(null!));
        Assert.Null(model.Editor.ActiveSession);
    }

    [Fact]
    public void RejectedCommitKeepsBothDraftFieldsAndCanCancelWithoutModification()
    {
        var committed = new TimetableCellValue("원래", "반");
        var session = new CellEditSession("독립 대상", committed, _ => false)
        {
            SubjectText = "새 교과", ClassText = "새 반"
        };
        Assert.False(session.TryApply());
        Assert.False(session.IsApplied);
        Assert.False(session.IsClosed);
        Assert.NotEmpty(session.ErrorText);
        Assert.Equal("새 교과", session.SubjectText);
        Assert.Equal("새 반", session.ClassText);
        Assert.Equal(new TimetableCellValue("원래", "반"), committed);
        session.Cancel();
        Assert.True(session.IsClosed);
    }

    [Fact]
    public void IndependentOwnersAndFreshRunsDoNotShareAcceptedValues()
    {
        var first = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var other = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var session = first.Editor.BeginEdit(first.Cells[0]);
        session.SubjectText = "실행 중";
        session.ClassText = "반";
        Assert.True(session.TryApply());
        Assert.Equal("", other.Cells[0].Value.SubjectText);
        Assert.Equal("", new WeeklyTimetableViewModel(WeeklyTimetable.Empty()).Cells[0].Value.ClassText);
    }

    [Fact]
    public void NullFieldsCannotEnterCanonicalValueOrDraft()
    {
        Assert.Throws<ArgumentNullException>(() => new TimetableCellValue(null!, ""));
        Assert.Throws<ArgumentNullException>(() => new TimetableCellValue("", null!));
        var value = new TimetableCellValue("", "");
        Assert.Throws<ArgumentNullException>(() => new CellEditSession(null!, value, _ => true));
        Assert.Throws<ArgumentNullException>(() => new CellEditSession("", null!, _ => true));
        Assert.Throws<ArgumentNullException>(() => new CellEditSession("", value, null!));
        var session = new CellEditSession("", value, _ => true);
        Assert.Throws<ArgumentNullException>(() => session.SubjectText = null!);
        Assert.Throws<ArgumentNullException>(() => session.ClassText = null!);
    }

    [Theory]
    [InlineData("", "", "")]
    [InlineData("교과", "", "교과")]
    [InlineData("", "반", "반")]
    [InlineData("교과", "반", "교과\n반")]
    [InlineData(" ", " ", " \n ")]
    [InlineData("\n", "반\n", "\n\n반\n")]
    [InlineData("<b>교과</b>", "  반  ", "<b>교과</b>\n  반  ")]
    public void DisplayUsesExactEmptyOnlyAndNeverReinterpretsText(string subject, string classText, string expected)
    {
        var value = new TimetableCellValue(subject, classText);
        Assert.Equal(expected, TimetableCellFormatter.Format(value));
        Assert.Equal(subject, value.SubjectText);
        Assert.Equal(classText, value.ClassText);
    }

    [Fact]
    public void IdenticalDisplayTextDoesNotCollapseDifferentCanonicalFieldBoundaries()
    {
        var week = WeeklyTimetable.Empty().WithCellValue(SchoolDay.Monday, 1, new("교과\n반", ""));
        var model = new WeeklyTimetableViewModel(week);
        var notifications = new List<string?>();
        model.Cells[0].PropertyChanged += (_, args) => notifications.Add(args.PropertyName);
        var session = model.Editor.BeginEdit(model.Cells[0]);
        session.SubjectText = "교과";
        session.ClassText = "반";
        Assert.True(session.TryApply());
        Assert.Equal("교과\n반", model.Cells[0].DisplayText);
        Assert.Equal(new TimetableCellValue("교과", "반"), model.Cells[0].Value);
        Assert.Equal(new[] { "Value" }, notifications);
        var reopen = model.Editor.BeginEdit(model.Cells[0]);
        Assert.Equal("교과", reopen.SubjectText);
        Assert.Equal("반", reopen.ClassText);
        reopen.Cancel();
    }

    [Fact]
    public void OldSingleTextCanBeCarriedWholeWithoutParsing()
    {
        const string legacy = "  물리학\n실험 A반!\r\n  ";
        var value = new TimetableCellValue(legacy, "");
        Assert.Equal(legacy, value.SubjectText);
        Assert.Equal("", value.ClassText);
        Assert.Equal(legacy, TimetableCellFormatter.Format(value));
    }
}
