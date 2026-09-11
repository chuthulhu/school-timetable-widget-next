using SchoolTimetableWidget.Desktop.Features.DisplaySettings;
using System.IO;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Desktop.Features.Persistence;

public interface IProfileStore
{
    ProfileLoadResult Load();
    void Save(ProfileSnapshot snapshot);
}

public enum ProfileLoadState { Missing, Loaded, Invalid, Unsupported, Unavailable }

public sealed record ProfileLoadResult(ProfileSnapshot Snapshot, ProfileLoadState State, string Path)
{
    public bool CanWrite => State is ProfileLoadState.Missing or ProfileLoadState.Loaded;
    public string Notice => CanWrite ? "" :
        "저장 데이터를 불러오지 못했습니다. 기본값으로 임시 실행 중이며 현재는 저장할 수 없습니다.\n" +
        "원본 저장파일은 수정하지 않았습니다. 파일 경로: " + Path;
}

/// <summary>Synchronous persisted-snapshot boundary. Owners publish only after a null (success) result.</summary>
public sealed class ProfileSession
{
    private readonly IProfileStore? _store;
    private bool _saving;
    public ProfileSession(IProfileStore store, ProfileSnapshot? firstRunDefaults = null)
    {
        _store = store;
        var result = store.Load();
        LoadResult = result.State == ProfileLoadState.Missing && firstRunDefaults is not null ? result with { Snapshot = firstRunDefaults } : result;
        Current = LoadResult.Snapshot;
    }
    private ProfileSession(string unavailablePath)
    {
        LoadResult = new(ProfileSnapshot.Defaults(), ProfileLoadState.Unavailable, unavailablePath);
        Current = LoadResult.Snapshot;
    }
    public static ProfileSession Unavailable(string path) => new(path);
    public ProfileLoadResult LoadResult { get; }
    public ProfileSnapshot Current { get; private set; }
    public string? SaveTimetable(WeeklyTimetable value) => Commit(new(value, Current.Schedule, Current.Overrides, Current.ShowLunch, Current.Display, Current.DisplayPresets));
    public string? SaveSchedule(PeriodSchedule value) => Commit(new(Current.Timetable, value, Current.Overrides, Current.ShowLunch, Current.Display, Current.DisplayPresets));
    public string? SaveOverrides(IReadOnlyCollection<DateSpecificOverride> value) => Commit(new(Current.Timetable, Current.Schedule, value, Current.ShowLunch, Current.Display, Current.DisplayPresets));
    public string? SaveLunch(bool value) => Commit(new(Current.Timetable, Current.Schedule, Current.Overrides, value, Current.Display, Current.DisplayPresets));

    public string? SaveDisplay(DisplayConfiguration value) => Commit(new(Current.Timetable, Current.Schedule, Current.Overrides, Current.ShowLunch, value, Current.DisplayPresets));

    public string? SaveDisplay(DisplayConfiguration value, UserDisplayPresetLibrary presets) =>
        Commit(new(Current.Timetable, Current.Schedule, Current.Overrides, Current.ShowLunch, value, presets));

    private string? Commit(ProfileSnapshot candidate)
    {
        if (!LoadResult.CanWrite) return "저장 데이터를 불러오지 못해 저장할 수 없습니다. 원본 파일을 보존하고 기존 임시 상태를 유지합니다.";
        if (_saving) return "다른 저장 작업 중입니다. 잠시 후 다시 적용해 주세요.";
        _saving = true;
        try
        {
            _store!.Save(candidate);
            Current = candidate;
            return null;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            System.Diagnostics.Debug.WriteLine(error);
            return "저장하지 못했습니다. 기존 내용은 유지됩니다. 저장 위치와 접근 권한을 확인한 뒤 다시 적용해 주세요.";
        }
        finally { _saving = false; }
    }
}
