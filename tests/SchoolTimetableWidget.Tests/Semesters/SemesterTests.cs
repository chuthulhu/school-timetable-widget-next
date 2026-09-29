using System.Text;
using System.Text.Json.Nodes;
using SchoolTimetableWidget.Core.Features.Semesters;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Core.Features.Timetable;
using SchoolTimetableWidget.Desktop.Features.Persistence;
using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using SchoolTimetableWidget.Desktop.Infrastructure.Persistence;
using SchoolTimetableWidget.Tests.Persistence;
using SchoolTimetableWidget.Tests.DisplaySettings;

namespace SchoolTimetableWidget.Tests.Semesters;

public class SemesterTests
{
    internal static JsonObject LegacyFields(JsonNode profile)
    {
        var data = profile["semesterSets"]![0]!.DeepClone().AsObject();
        data.Remove("semesterId"); data.Remove("displayName");
        data["presentation"] = profile["presentation"]!.DeepClone();
        return data;
    }
    internal static ProfileSnapshot Two()
    {
        var original = ProfileBackupFileTests.Sample();
        var a = original.ActiveSemester.WithName("2026 1학기");
        var b = new SemesterSet(Guid.NewGuid(), "2026 2학기", WeeklyTimetable.Empty().WithCellValue(SchoolDay.Monday, 1, new("통합과학", "2-1")),
            new(DefaultPeriodSchedule.Periods.Select(p => new PeriodDefinition(p.PeriodNumber, p.Start.AddMinutes(-20), p.End.AddMinutes(-20)))), []);
        return original.WithSemesters([a,b], a.SemesterId);
    }
    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public void OldProfilesMigrateExactlyWithoutStartupRewriteAndSaveV5(int version)
    {
        using var temp = new TempProfile(); var bytes = UserPresetPersistenceTests.Fixture(version); File.WriteAllBytes(temp.File, bytes);
        var modified = File.GetLastWriteTimeUtc(temp.File);
        using var store = new JsonProfileStore(temp.Directory); var session = new ProfileSession(store);
        var item = Assert.Single(session.Current.SemesterSets);
        Assert.Equal("기본 학기", item.DisplayName); Assert.Equal(item.SemesterId, session.Current.ActiveSemesterId);
        var old = JsonNode.Parse(bytes)!["profile"]!;
        var now = JsonNode.Parse(ProfileJson.Serialize(session.Current))!["profile"]!;
        foreach (var field in new[] { "timetable", "periodSchedule", "dateOverrides" })
            Assert.True(JsonNode.DeepEquals(old[field], now["semesterSets"]![0]![field]));
        Assert.True(JsonNode.DeepEquals(old["presentation"], now["presentation"]));
        if (version == 4)
            foreach (var field in new[] { "display", "displayPresets" }) Assert.True(JsonNode.DeepEquals(old[field], now[field]));
        var canonical = ProfileJson.Serialize(session.Current);
        Assert.Equal(bytes, File.ReadAllBytes(temp.File)); Assert.Equal(modified, File.GetLastWriteTimeUtc(temp.File));
        Assert.Null(session.SaveLunch(session.Current.ShowLunch));
        Assert.Equal(canonical, File.ReadAllBytes(temp.File));
        Assert.Equal(5, JsonNode.Parse(canonical)!["schemaVersion"]!.GetValue<int>());
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CreationCopiesOnlyApprovedBaseDataAndActivatesAfterSave(bool copy)
    {
        using var temp = new TempProfile(); using var store = new JsonProfileStore(temp.Directory);
        var session = new ProfileSession(store, ProfileBackupFileTests.Sample()); var runtime = new ProfileRuntime(session, () => { }, _ => { });
        var old = session.Current;
        Assert.Null(runtime.Semesters.Create("  겨울방학 😀  ", copy));
        var next = session.Current.ActiveSemester;
        Assert.Equal(2, session.Current.SemesterSets.Count); Assert.NotEqual(old.ActiveSemesterId, next.SemesterId);
        Assert.Equal("겨울방학 😀", next.DisplayName); Assert.Empty(next.Overrides);
        Assert.Same(old.Schedule, next.Schedule);
        Assert.Equal(copy ? old.Timetable.Cells.Select(c => c.Value) : WeeklyTimetable.Empty().Cells.Select(c => c.Value), next.Timetable.Cells.Select(c => c.Value));
        Assert.Same(old.ActiveSemester, session.Current.SemesterSets[0]);
        Assert.Same(old.Display, session.Current.Display); Assert.Same(old.DisplayPresets, session.Current.DisplayPresets); Assert.Equal(old.ShowLunch, session.Current.ShowLunch);
        Assert.Equal(ProfileJson.Serialize(session.Current), File.ReadAllBytes(temp.File));
    }
    [Theory]
    [InlineData("")] [InlineData(" \t ")] [InlineData("기본 학기")] [InlineData("SUMMER")]
    public void InvalidOrDuplicateNamesLeaveAllStateUntouched(string name)
    {
        using var temp = new TempProfile(); using var store = new JsonProfileStore(temp.Directory);
        var session = new ProfileSession(store); var runtime = new ProfileRuntime(session, () => { }, _ => { });
        Assert.Null(runtime.Semesters.Create("summer", false)); var before = session.Current; var bytes = File.ReadAllBytes(temp.File);
        Assert.NotNull(runtime.Semesters.Create(name, false)); Assert.Same(before, session.Current); Assert.Equal(bytes, File.ReadAllBytes(temp.File));
        Assert.NotNull(runtime.Semesters.Rename(before.ActiveSemesterId, new string('가',81))); Assert.Same(before, session.Current);
    }
    [Fact]
    public void RenameDeleteOrderingAndRestartPreserveStableIdentity()
    {
        using var temp = new TempProfile(); byte[] expected;
        using (var store = new JsonProfileStore(temp.Directory))
        {
            var session = new ProfileSession(store); var runtime = new ProfileRuntime(session, () => { }, _ => { });
            var a = session.Current.ActiveSemesterId;
            Assert.NotNull(runtime.Semesters.Delete(a,true)); Assert.Null(runtime.Semesters.Create("B",false));
            var b = session.Current.ActiveSemesterId; var data = session.Current.ActiveSemester;
            Assert.Null(runtime.Semesters.Rename(b,"보충수업용")); Assert.Equal(b,session.Current.ActiveSemesterId);
            Assert.Same(data.Timetable,session.Current.Timetable); Assert.Same(data.Schedule,session.Current.Schedule);
            Assert.NotNull(runtime.Semesters.Delete(b,true)); Assert.NotNull(runtime.Semesters.Delete(a,false));
            Assert.Null(runtime.Semesters.Create("C",true)); var c=session.Current.ActiveSemesterId;
            Assert.Equal(new[]{a,b,c},session.Current.SemesterSets.Select(s=>s.SemesterId));
            Assert.Null(runtime.Semesters.Delete(b,true)); Assert.Equal(new[]{a,c},session.Current.SemesterSets.Select(s=>s.SemesterId));
            Assert.Null(runtime.Semesters.Activate(a)); expected=ProfileJson.Serialize(session.Current);
        }
        using var restart=new JsonProfileStore(temp.Directory); Assert.Equal(expected,ProfileJson.Serialize(restart.Load().Snapshot));
    }
    [Theory]
    [InlineData("switch")] [InlineData("create")] [InlineData("rename")] [InlineData("delete")]
    public void FailedSemesterTransactionNeverPublishes(string operation)
    {
        using var temp = new TempProfile(); bool fail=false; using var store=new JsonProfileStore(temp.Directory,_=>{if(fail)throw new IOException("fault");});
        var session=new ProfileSession(store,Two()); Assert.Null(session.SaveLunch(true)); int refresh=0;
        var runtime=new ProfileRuntime(session,()=>refresh++,_=>{}); var old=session.Current; var bytes=File.ReadAllBytes(temp.File); fail=true;
        var b=old.SemesterSets[1].SemesterId;
        var error=operation switch {"switch"=>runtime.Semesters.Activate(b),"create"=>runtime.Semesters.Create("new",true),"rename"=>runtime.Semesters.Rename(b,"renamed"),_=>runtime.Semesters.Delete(b,true)};
        Assert.NotNull(error); Assert.Same(old,session.Current); Assert.Equal(old.ActiveSemesterId,runtime.Semesters.ActiveSemesterId);
        Assert.Same(old.Timetable,runtime.Timetable.CommittedTimetable); Assert.Same(old.Schedule,runtime.Schedule.Current);
        Assert.Equal(bytes,File.ReadAllBytes(temp.File)); Assert.Equal(0,refresh);
    }
    [Theory]
    [InlineData("empty")] [InlineData("null")] [InlineData("nullEntry")] [InlineData("duplicateId")] [InlineData("duplicateName")]
    [InlineData("blank")] [InlineData("long")] [InlineData("invalidId")] [InlineData("dangling")] [InlineData("nullActive")]
    [InlineData("badOtherWeek")] [InlineData("badOtherSchedule")] [InlineData("badOtherOverride")]
    public void InvalidV5RejectsWholeProfileAndPreservesBytes(string kind)
    {
        var node=JsonNode.Parse(ProfileJson.Serialize(Two()))!;var p=node["profile"]!;var items=p["semesterSets"]!.AsArray();
        switch(kind)
        {
            case "empty":items.Clear();break;case "null":p["semesterSets"]=null;break;case "nullEntry":items[1]=null;break;
            case "duplicateId":items[1]!["semesterId"]=items[0]!["semesterId"]!.DeepClone();break;
            case "duplicateName":items[1]!["displayName"]=items[0]!["displayName"]!.DeepClone();break;
            case "blank":items[1]!["displayName"]="  ";break;case "long":items[1]!["displayName"]=new string('x',81);break;
            case "invalidId":items[1]!["semesterId"]=Guid.Empty.ToString();break;case "dangling":p["activeSemesterId"]=Guid.NewGuid().ToString();break;
            case "nullActive":p["activeSemesterId"]=null;break;case "badOtherWeek":items[1]!["timetable"]!.AsArray().RemoveAt(0);break;
            case "badOtherSchedule":items[1]!["periodSchedule"]![0]!["end"]="00:00:00.0000000";break;
            case "badOtherOverride":items[1]!["dateOverrides"]!.AsArray().Add(items[0]!["dateOverrides"]![0]!.DeepClone());items[1]!["dateOverrides"]![0]!["date"]="2026-09-12";break;
        }
        using var temp=new TempProfile();var bytes=Encoding.UTF8.GetBytes(node.ToJsonString());File.WriteAllBytes(temp.File,bytes);
        using var store=new JsonProfileStore(temp.Directory);var session=new ProfileSession(store);
        Assert.Equal(ProfileLoadState.Invalid,session.LoadResult.State);Assert.Single(session.Current.SemesterSets);
        Assert.NotNull(session.SaveLunch(true));Assert.Equal(bytes,File.ReadAllBytes(temp.File));
    }
    [Fact]
    public void FullBackupAndPreRestoreSnapshotContainEverySemesterAndActiveId()
    {
        using var temp=new TempProfile();using var store=new JsonProfileStore(temp.Directory);var session=new ProfileSession(store,Two());
        var runtime=new ProfileRuntime(session,()=>{},_=>{});Assert.Null(runtime.Semesters.Activate(session.Current.SemesterSets[1].SemesterId));
        var old=ProfileJson.Serialize(session.Current);var backup=ProfileBackupFile.Export(session.Current);
        Assert.Equal(old,ProfileJson.Serialize(ProfileBackupFile.Import(backup)));
        Assert.Null(runtime.Restore(ProfileSnapshot.Defaults()));
        Assert.Equal(old,File.ReadAllBytes(new ProfileRecoveryFiles(temp.Directory).SnapshotPath));
        Assert.Null(runtime.Restore(ProfileBackupFile.Import(backup)));Assert.Equal(old,File.ReadAllBytes(temp.File));
    }
    [Fact]
    public void OldV1BackupWithV4ProfileRestoresAsOneNeutralSemester()
    {
        var old=JsonNode.Parse(UserPresetPersistenceTests.Fixture(4))!;
        var backup=new JsonObject { ["backupFileVersion"]=1,["profileSchemaVersion"]=4,["profile"]=old["profile"]!.DeepClone() };
        var restored=ProfileBackupFile.Import(Encoding.UTF8.GetBytes(backup.ToJsonString()));
        Assert.Equal("기본 학기",Assert.Single(restored.SemesterSets).DisplayName);
        Assert.Equal(ProfileJson.Serialize(ProfileJson.Deserialize(UserPresetPersistenceTests.Fixture(4))),ProfileJson.Serialize(restored));
    }
}
