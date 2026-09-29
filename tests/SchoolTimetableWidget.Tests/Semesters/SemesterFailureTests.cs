using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Features.Semesters;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Tests.Persistence;
using SchoolTimetableWidget.Tests.Timetable;
using SchoolTimetableWidget.Tests.Time;
using SchoolTimetableWidget.Core.Time;

namespace SchoolTimetableWidget.Tests.Semesters;

public class SemesterFailureTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void FullRestoreFailureRetainsAllSemesters(int stage)
    {
        using var temp=new TempProfile();using var store=new JsonProfileStore(temp.Directory);var session=new ProfileSession(store,SemesterTests.Two());
        Assert.Null(session.SaveLunch(true));var runtime=new ProfileRuntime(session,()=>{},_=>{});var before=File.ReadAllBytes(temp.File);
        store.RestoreCheckpoint=s=>{if((int)s==stage)throw new IOException("fault");};
        Assert.NotNull(runtime.Restore(ProfileSnapshot.Defaults()));Assert.Equal(before,File.ReadAllBytes(temp.File));
        Assert.Equal(before,ProfileJson.Serialize(session.Current));Assert.Equal(2,runtime.Semesters.Items.Count);Assert.False(session.IsRecoveryRequired);
    }
    [Fact]
    public void RecoveryRequiredRestartPreservesAndRecoversAllSemesters()
    {
        using var temp=new TempProfile();byte[] previous;
        using(var store=new JsonProfileStore(temp.Directory))
        {
            var session=new ProfileSession(store,SemesterTests.Two());Assert.Null(session.SaveLunch(true));previous=File.ReadAllBytes(temp.File);
            store.RestoreCheckpoint=s=>{if(s is RestoreStage.PublishCandidate or RestoreStage.RollbackWrite)throw new IOException("fault");};
            var runtime=new ProfileRuntime(session,()=>{},_=>{});Assert.NotNull(runtime.Restore(ProfileSnapshot.Defaults()));
            Assert.True(session.IsRecoveryRequired);Assert.NotNull(runtime.Semesters.Create("blocked",false));
            Assert.Equal(previous,ProfileJson.Serialize(session.Current));
        }
        using var restart=new JsonProfileStore(temp.Directory);var current=new ProfileSession(restart);var restored=new ProfileRuntime(current,()=>{},_=>{});
        Assert.True(current.IsRecoveryRequired);Assert.Equal(previous,ProfileJson.Serialize(current.Current));
        Assert.Null(restored.Recover());Assert.Equal(previous,File.ReadAllBytes(temp.File));Assert.Equal(2,restored.Semesters.Items.Count);
        Assert.False(current.IsRecoveryRequired);
    }
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void SelectorFailureResetsBindingWithoutChangingHeaderHighlightOrDisk(int stage) => HighlightTestDispatcher.Run(()=>
    {
        using var temp=new TempProfile();bool fail=false;using var store=new JsonProfileStore(temp.Directory,s=>{if(fail&&(int)s==stage)throw new IOException("fault");});
        var session=new ProfileSession(store,SemesterTests.Two());Assert.Null(session.SaveLunch(true));
        var clock=new FakeApplicationClock(new(new DateTimeOffset(2026,9,21,9,35,0,TimeSpan.FromHours(9)),ApplicationTimeSource.PcLocalFallback,0));
        var header=new CurrentStatusHeaderViewModel();CurrentStatusRefreshLoop? loop=null;
        var runtime=new ProfileRuntime(session,()=>loop!.RefreshNow(),_=>{});
        using(loop=new(clock,runtime.Resolve,header,runtime.Timetable,()=>runtime.Lunch.Enabled))
        {
            loop.RefreshNow();var before=session.Current;var bytes=File.ReadAllBytes(temp.File);var status=header.StatusText;
            var cells=runtime.Timetable.Cells.Select(c=>(c.Value,c.IsCurrent)).ToArray();var owner=new Window();var errors=new List<string>();
            var selector=new SemesterSelector(runtime.Semesters,owner,errors.Add);fail=true;
            selector.Selector.SetCurrentValue(Selector.SelectedValueProperty,before.SemesterSets[1].SemesterId);
            Assert.Single(errors);Assert.Equal(before.ActiveSemesterId,selector.Selector.SelectedValue);
            Assert.Same(before,session.Current);Assert.Equal(status,header.StatusText);Assert.Equal(cells,runtime.Timetable.Cells.Select(c=>(c.Value,c.IsCurrent)));
            Assert.Equal(bytes,File.ReadAllBytes(temp.File));Assert.NotNull(selector.Selector.GetBindingExpression(Selector.SelectedValueProperty));owner.Close();
        }
    });
    [Fact]
    public void SemesterMutationsLeaveMachineFilesAndGlobalPreviewUntouched()
    {
        using var temp=new TempProfile();var names=new[]{"window-state.json","tray-state.json","font-cache-sentinel"};
        foreach(var name in names)File.WriteAllText(Path.Combine(temp.Directory,name),"sentinel "+name);
        var original=names.ToDictionary(n=>n,n=>(File.ReadAllBytes(Path.Combine(temp.Directory,n)),File.GetLastWriteTimeUtc(Path.Combine(temp.Directory,n))));
        using var store=new JsonProfileStore(temp.Directory);var session=new ProfileSession(store,SemesterTests.Two());var runtime=new ProfileRuntime(session,()=>{},_=>{});
        var display=session.Current.Display;var presets=session.Current.DisplayPresets;var lunch=session.Current.ShowLunch;var b=session.Current.SemesterSets[1].SemesterId;
        Assert.Null(runtime.Semesters.Create("new",true));Assert.Null(runtime.Semesters.Activate(session.Current.SemesterSets[0].SemesterId));
        Assert.Null(runtime.Semesters.Delete(b,true));Assert.Null(session.SaveLunch(lunch));Assert.Null(session.SaveDisplay(display,presets));
        Assert.Equal(2,session.Current.SemesterSets.Count);Assert.Same(display,session.Current.Display);Assert.Same(presets,session.Current.DisplayPresets);
        Assert.Equal(lunch,session.Current.ShowLunch);
        foreach(var name in names){Assert.Equal(original[name].Item1,File.ReadAllBytes(Path.Combine(temp.Directory,name)));Assert.Equal(original[name].Item2,File.GetLastWriteTimeUtc(Path.Combine(temp.Directory,name)));}
    }
}
