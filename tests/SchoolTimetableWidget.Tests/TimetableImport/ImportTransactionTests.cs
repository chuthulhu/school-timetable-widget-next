using System.Runtime.InteropServices;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Core.Features.TimetableImport;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Desktop.Features.TimetableImport;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;

namespace SchoolTimetableWidget.Tests.TimetableImport;

public class ImportTransactionTests
{
    [Fact]
    public void ReadingSelectingAndConfirmingDoNotMutateOriginalAndCancelIsTerminal()
    {
        var model = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var original = model.CommittedTimetable;
        var actions = new TimetableImportActions(new MemoryClipboard());
        var session = actions.CreateSession(model, TimetableImportMode.School);
        session.LoadText(ImportFixtures.SchoolText());
        Assert.Null(session.SelectedCandidate);
        Assert.False(session.CanApply);
        session.SelectedCandidate = session.Candidates[1];
        Assert.False(session.CanApply);
        session.MappingConfirmed = true;
        Assert.True(session.CanApply);
        Assert.Equal(35, session.Preview!.Cells.Count);
        Assert.Same(original, model.CommittedTimetable);
        Assert.All(model.Cells, c => Assert.Equal(new("", ""), c.Value));
        session.CancelCommand.Execute(null);
        Assert.True(session.IsClosed);
        Assert.False(session.TryApply());
        session.LoadText(CanonicalTimetableImporter.CreateTemplate());
        Assert.Same(original, model.CommittedTimetable);
    }

    [Fact]
    public void EveryNotificationSeesAllThirtyFiveReplacementsAndStableHighlight()
    {
        var model = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var controls = model.Cells.ToArray();
        var original = model.CommittedTimetable;
        model.SetCurrentCell((SchoolDay.Wednesday, 4));
        var session = new TimetableImportActions(new MemoryClipboard()).CreateSession(model, TimetableImportMode.School);
        session.LoadText(ImportFixtures.SchoolText());
        session.SelectedCandidate = session.Candidates[1]; session.MappingConfirmed = true;
        var replacement = session.SelectedCandidate.Timetable;
        var notifications = 0;
        foreach (var cell in model.Cells)
            cell.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is not ("Value" or "DisplayText")) return;
                notifications++;
                Assert.Same(replacement, model.CommittedTimetable);
                Assert.Equal(replacement.Cells.Select(c => c.Value), model.Cells.Select(c => c.Value));
                Assert.Equal(replacement.Cells.Select(c => TimetableCellFormatter.Format(c.Value)), model.Cells.Select(c => c.DisplayText));
                Assert.True(model.Cells[17].IsCurrent);
                Assert.Single(model.Cells, c => c.IsCurrent);
                Assert.False(session.TryApply());
                Assert.False(model.TryReplaceTimetable(replacement, original));
            };
        Assert.True(session.TryApply());
        Assert.Equal(70, notifications);
        Assert.True(session.IsApplied);
        Assert.Equal(controls, model.Cells);
        Assert.All(original.Cells, c => Assert.Equal(new("", ""), c.Value));
        session.Cancel();
        Assert.Same(replacement, model.CommittedTimetable);
    }

    [Fact]
    public void SwitchingCandidateRequiresNewConfirmationAndRefreshesPreview()
    {
        var session = new TimetableImportSession(TimetableImportMode.School, _ => true);
        session.LoadText(ImportFixtures.SchoolText());
        session.SelectedCandidate = session.Candidates[0]; session.MappingConfirmed = true;
        var previous = session.Preview;
        session.SelectedCandidate = session.Candidates[1];
        Assert.False(session.MappingConfirmed); Assert.False(session.CanApply);
        Assert.NotSame(previous, session.Preview);
        Assert.Equal(session.Candidates[1].Timetable.Cells.Select(c => c.Value), session.Preview!.Cells.Select(c => c.Value));
    }

    [Fact]
    public void SingleCellEditingWorksAfterImportAndOldSessionsCannotOverwriteNewWeek()
    {
        var model = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var actions = new TimetableImportActions(new MemoryClipboard());
        var session = actions.CreateSession(model, TimetableImportMode.School);
        session.LoadText(ImportFixtures.SchoolText(1)); session.SelectedCandidate = session.Candidates[0]; session.MappingConfirmed = true;
        var edit = model.Editor.BeginEdit(model.Cells[0]);
        Assert.False(session.TryApply());
        edit.Cancel();
        Assert.True(session.TryApply());
        var replacement = model.CommittedTimetable;
        edit = model.Editor.BeginEdit(model.Cells[34]);
        Assert.Equal(replacement.Cells[34].Value, edit.OriginalValue);
        edit.SubjectText = "수정"; edit.ClassText = "새 반";
        Assert.True(edit.TryApply());
        Assert.Equal(new("수정", "새 반"), model.Cells[34].Value);
        Assert.Equal(replacement.Cells.Take(34).Select(c => c.Value), model.Cells.Take(34).Select(c => c.Value));
        Assert.False(session.TryApply());
    }

    [Fact]
    public void StaleWholeWeekBaselineRejectsBeforeChangingAnySlot()
    {
        var model = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var session = new TimetableImportActions(new MemoryClipboard()).CreateSession(model, TimetableImportMode.Canonical);
        session.LoadText(CanonicalTimetableImporter.CreateTemplate());
        var edit = model.Editor.BeginEdit(model.Cells[12]); edit.SubjectText = "다른 변경"; Assert.True(edit.TryApply());
        var current = model.CommittedTimetable;
        Assert.False(session.TryApply()); Assert.False(session.IsClosed); Assert.NotEmpty(session.ErrorText);
        Assert.Same(current, model.CommittedTimetable);
        Assert.Equal(current.Cells.Select(c => c.Value), model.Cells.Select(c => c.Value));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedReReadClearsOldCandidateAndCannotApplyStaleData(bool clipboardFailure)
    {
        var model = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var original = model.CommittedTimetable;
        var clipboard = new MemoryClipboard { Text = CanonicalTimetableImporter.CreateTemplate() };
        var actions = new TimetableImportActions(clipboard);
        var session = actions.CreateSession(model, TimetableImportMode.Canonical);
        actions.ReadClipboard(session); Assert.True(session.CanApply);
        clipboard.Fail = clipboardFailure; clipboard.Text = "bad input";
        actions.ReadClipboard(session);
        Assert.Empty(session.Candidates); Assert.Null(session.SelectedCandidate); Assert.Null(session.Preview);
        Assert.NotEmpty(session.ErrorText); Assert.False(session.TryApply());
        Assert.Same(original, model.CommittedTimetable);
    }

    [Fact]
    public void TemplateCopyOnlyWritesWhenExplicitlyInvokedAndReportsFailure()
    {
        var clipboard = new MemoryClipboard();
        var actions = new TimetableImportActions(clipboard);
        Assert.Equal(0, clipboard.Writes);
        Assert.Contains("복사했습니다", actions.CopyTemplate());
        Assert.Equal(1, clipboard.Writes);
        Assert.Equal(35, CanonicalTimetableImporter.Import(ClipboardTable.Parse(clipboard.Text)).Timetable.Cells.Count);
        clipboard.Fail = true;
        Assert.Contains("복사하지 못했습니다", actions.CopyTemplate());
        Assert.Equal(1, clipboard.Writes);
    }

    [Fact]
    public void NullReplacementRejectsAndFreshOwnerHasNoPersistence()
    {
        var model = new WeeklyTimetableViewModel(WeeklyTimetable.Empty());
        var original = model.CommittedTimetable;
        Assert.Throws<ArgumentNullException>(() => model.TryReplaceTimetable(original, null!));
        Assert.Same(original, model.CommittedTimetable);
        Assert.True(model.TryReplaceTimetable(original, SchoolTimetableImporter.Import(new(ImportFixtures.School()))[0].Timetable));
        Assert.All(new WeeklyTimetableViewModel(WeeklyTimetable.Empty()).Cells, c => Assert.Equal(new("", ""), c.Value));
    }

    internal sealed class MemoryClipboard : ISpreadsheetClipboard
    {
        public string Text { get; set; } = "";
        public bool Fail { get; set; }
        public int Writes { get; private set; }
        public string ReadText() => Fail ? throw new ExternalException("busy") : Text;
        public void WriteText(string text)
        {
            if (Fail) throw new ExternalException("busy");
            Text = text; Writes++;
        }
    }
}
