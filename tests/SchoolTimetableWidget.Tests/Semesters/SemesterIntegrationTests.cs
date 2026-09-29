using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.TimetableImport;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Features.Semesters;
using SchoolTimetableWidget.Desktop.Features.TimetableImport;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Desktop.Infrastructure.Windows;
using SchoolTimetableWidget.Tests.Persistence;
using SchoolTimetableWidget.Tests.Timetable;
using SchoolTimetableWidget.Tests.Time;

namespace SchoolTimetableWidget.Tests.Semesters;

public class SemesterIntegrationTests
{
    [Theory]
    [InlineData("cell")] [InlineData("period")] [InlineData("date")] [InlineData("import")]
    public void EditingOneSemesterLeavesOtherExact(string kind)
    {
        using var temp=new TempProfile();using var store=new JsonProfileStore(temp.Directory);var session=new ProfileSession(store,SemesterTests.Two());
        var runtime=new ProfileRuntime(session,()=>{},_=>{});var a=session.Current.ActiveSemesterId;var b=session.Current.SemesterSets[1];
        runtime.Timetable.UpdateCurrent(new(2026,9,21),null);
        switch(kind)
        {
            case "cell":var edit=runtime.Timetable.Editor.BeginEdit(runtime.Timetable.Cells[0]);edit.SubjectText="변경";Assert.True(edit.TryApply());break;
            case "period":var period=runtime.ScheduleEditor.CreateSession();period.Rows[0].StartText="08:55";Assert.True(period.TryApply());break;
            case "date":var date=runtime.DateEditor.CreateSession(new(2026,9,21));date.UseTimetable=true;date.UseSchedule=true;date.TimetableRows[0].SubjectText="날짜";Assert.True(date.TryApply());break;
            case "import":var import=new TimetableImportActions(new UnusedClipboard()).CreateSession(runtime.Timetable,TimetableImportMode.Canonical);
                Assert.Equal("2026 1학기",import.TargetSemesterName);import.LoadText(CanonicalTimetableImporter.CreateTemplate());Assert.True(import.TryApply());break;
        }
        Assert.Same(b,session.Current.SemesterSets[1]);var changed=session.Current.ActiveSemester;
        Assert.Null(runtime.Semesters.Activate(b.SemesterId));Assert.Same(b.Timetable,runtime.Timetable.CommittedTimetable);Assert.Same(b.Schedule,runtime.Schedule.Current);
        Assert.Null(runtime.Semesters.Activate(a));Assert.Same(changed.Timetable,runtime.Timetable.CommittedTimetable);Assert.Same(changed.Schedule,runtime.Schedule.Current);
    }
    [Fact]
    public void SameDateNamespaceAndRemovalRemainIndependent()
    {
        using var temp=new TempProfile();using var store=new JsonProfileStore(temp.Directory);var session=new ProfileSession(store,SemesterTests.Two());
        var runtime=new ProfileRuntime(session,()=>{},_=>{});var a=session.Current.ActiveSemesterId;var b=session.Current.SemesterSets[1].SemesterId;var day=new DateOnly(2026,9,21);
        foreach(var entry in new[]{(a,"A"),(b,"B")})
        {
            Assert.Null(runtime.Semesters.Activate(entry.Item1));var edit=runtime.DateEditor.CreateSession(day);edit.UseTimetable=true;edit.UseSchedule=true;
            edit.TimetableRows[0].SubjectText=entry.Item2;edit.ScheduleDraft.Rows[0].StartText=entry.Item2=="A"?"08:10":"08:20";Assert.True(edit.TryApply());
        }
        Assert.Equal("B",runtime.Resolve(day).Timetable[SchoolDay.Monday,1].Value.SubjectText);
        Assert.Null(runtime.Semesters.Activate(a));Assert.Equal("A",runtime.Resolve(day).Timetable[SchoolDay.Monday,1].Value.SubjectText);
        Assert.Equal(new TimeOnly(8,10),runtime.Resolve(day).Schedule.Periods[0].Start);
        var remove=runtime.DateEditor.CreateSession(day);remove.UseTimetable=false;remove.UseSchedule=false;Assert.True(remove.TryApply());
        Assert.Null(runtime.Overrides.Get(day));Assert.Null(runtime.Semesters.Activate(b));Assert.Equal("B",runtime.Overrides.Get(day)!.Timetable![1].SubjectText);
    }
    [Theory]
    [InlineData("period")] [InlineData("date")] [InlineData("import")]
    public void OpenEditorsNeverRetargetEvenWhenCopySharesBaseReferences(string kind)
    {
        using var temp=new TempProfile();using var store=new JsonProfileStore(temp.Directory);var session=new ProfileSession(store);
        var runtime=new ProfileRuntime(session,()=>{},_=>{});Func<bool> apply;
        if(kind=="period") {var draft=runtime.ScheduleEditor.CreateSession();draft.Rows[0].StartText="08:00";apply=draft.TryApply;}
        else if(kind=="date") {var draft=runtime.DateEditor.CreateSession(new(2026,9,21));draft.UseTimetable=true;apply=draft.TryApply;}
        else {var draft=new TimetableImportActions(new UnusedClipboard()).CreateSession(runtime.Timetable,TimetableImportMode.Canonical);draft.LoadText(CanonicalTimetableImporter.CreateTemplate());apply=draft.TryApply;}
        Assert.Null(runtime.Semesters.Create("B",true));var current=session.Current;var bytes=File.ReadAllBytes(temp.File);
        Assert.False(apply());Assert.Same(current,session.Current);Assert.Equal(bytes,File.ReadAllBytes(temp.File));
    }
    [Fact]
    public void OpenCellEditorBlocksSemesterSwitchAndKeepsProvenance()
    {
        using var temp=new TempProfile();using var store=new JsonProfileStore(temp.Directory);var session=new ProfileSession(store,SemesterTests.Two());
        var runtime=new ProfileRuntime(session,()=>{},_=>{});runtime.Timetable.UpdateCurrent(new(2026,9,7),null);
        var edit=runtime.Timetable.Editor.BeginEdit(runtime.Timetable.Cells[0]);Assert.Contains("2026년 09월 07일",edit.TargetLabel);
        Assert.NotNull(runtime.Semesters.Activate(session.Current.SemesterSets[1].SemesterId));edit.SubjectText="예외 편집";Assert.True(edit.TryApply());
        Assert.Equal("예외 편집",session.Current.Overrides[0].Timetable![1].SubjectText);
    }
    [Fact]
    public void OneClockCycleUsesActiveSnapshotAndSwitchPreservesWeekAndGlobalDisplay() => HighlightTestDispatcher.Run(()=>
    {
        using var temp=new TempProfile();using var store=new JsonProfileStore(temp.Directory);var initial=SemesterTests.Two();
        // No current-date overrides: A is in period 1 while B is between periods at the same instant.
        initial=initial.ReplaceSemester(initial.ActiveSemester.WithOverrides([]));var session=new ProfileSession(store,initial);
        var clock=new FakeApplicationClock(new(new DateTimeOffset(2026,9,21,9,35,0,TimeSpan.FromHours(9)),ApplicationTimeSource.PcLocalFallback,0));
        var header=new CurrentStatusHeaderViewModel();CurrentStatusRefreshLoop? loop=null;
        var runtime=new ProfileRuntime(session,()=>loop!.RefreshNow(),_=>{});
        using(loop=new(clock,runtime.Resolve,header,runtime.Timetable,()=>runtime.Lunch.Enabled))
        {
            loop.RefreshNow();Assert.Contains("1교시",header.StatusText);Assert.Single(runtime.Timetable.Cells,c=>c.IsCurrent);
            var display=runtime.Display.Current;var presets=runtime.Display.CommittedPresets;var week=runtime.Timetable.ViewedWeekStart;var reads=clock.ReadCount;
            var b=session.Current.SemesterSets[1].SemesterId;Assert.Null(runtime.Semesters.Activate(b));
            Assert.Equal(reads+1,clock.ReadCount);Assert.Equal(b,loop.CurrentConfiguration!.SemesterId);
            Assert.Same(session.Current.Schedule,loop.CurrentConfiguration.Schedule);Assert.Contains("쉬는시간",header.StatusText);
            Assert.DoesNotContain(runtime.Timetable.Cells,c=>c.IsCurrent);Assert.Equal(week,runtime.Timetable.ViewedWeekStart);
            Assert.Same(display,runtime.Display.Current);Assert.Same(presets,runtime.Display.CommittedPresets);
            runtime.Timetable.NextWeekCommand.Execute(null);week=runtime.Timetable.ViewedWeekStart;
            Assert.Null(runtime.Semesters.Activate(initial.ActiveSemesterId));Assert.Equal(week,runtime.Timetable.ViewedWeekStart);Assert.DoesNotContain(runtime.Timetable.Cells,c=>c.IsCurrent);
        }
    });
    [Fact]
    public void SelectorAndManagementObjectsFollowCommittedIdAndProtectActiveDelete() => HighlightTestDispatcher.Run(()=>
    {
        using var temp=new TempProfile();using var store=new JsonProfileStore(temp.Directory);var session=new ProfileSession(store,SemesterTests.Two());
        var runtime=new ProfileRuntime(session,()=>{},_=>{});var owner=new Window();var selector=new SemesterSelector(runtime.Semesters,owner,error=>Assert.Fail(error));
        selector.Measure(new Size(500,100));selector.Arrange(new Rect(0,0,500,100));
        Assert.Equal(session.Current.ActiveSemesterId,selector.Selector.SelectedValue);Assert.Equal(2,selector.Selector.Items.Count);
        selector.Selector.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedValueProperty,session.Current.SemesterSets[1].SemesterId);
        Assert.Equal(session.Current.ActiveSemesterId,selector.Selector.SelectedValue);Assert.Equal("2026 2학기",session.Current.ActiveSemester.DisplayName);
        var manager=new SemesterManagementWindow(runtime.Semesters);Assert.False(manager.DeleteButton.IsEnabled);
        manager.SemesterList.SelectedItem=session.Current.SemesterSets[0];Assert.True(manager.DeleteButton.IsEnabled);
        var create=new SemesterNameWindow(null,(_,_)=>"검증 실패");Assert.False(create.CopyOption.IsChecked);create.NameInput.Text="학기";
        create.SaveButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));Assert.Equal("검증 실패",create.ErrorLabel.Text);
        var rename=new SemesterNameWindow("2026 2학기",(_,_)=>null);Assert.Equal(Visibility.Collapsed,rename.CopyOption.Visibility);
        Assert.Equal("2026 2학기",rename.NameInput.Text);create.Close();rename.Close();manager.Close();owner.Close();
    });
    private sealed class UnusedClipboard:ISpreadsheetClipboard
    {
        public string ReadText()=>throw new InvalidOperationException();
        public void WriteText(string value)=>throw new InvalidOperationException();
    }
}
