namespace SchoolTimetableWidget.Desktop.Features.Persistence;

public sealed record ProfileRestoreResult(ProfileLoadResult State, string? Error);
public interface IProfileRecoveryStore
{
    bool CanRestore { get; }
    bool CanRecover { get; }
    ProfileRestoreResult Restore(ProfileSnapshot previous, ProfileSnapshot candidate, Action<ProfileSnapshot> publish);
    ProfileRestoreResult Recover(ProfileSnapshot temporary, Action<ProfileSnapshot> publish);
    void ExportBackup(ProfileSnapshot current, string destination);
}
