using SchoolTimetableWidget.Desktop.Features.Persistence;
using System.IO;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Persistence;

public sealed partial class JsonProfileStore
{
    private ProfileLoadState _state;
    private bool _recoveryRequired;
    private ProfileRecoveryFiles Recovery => new(DirectoryPath);
    internal Action<RestoreStage>? RestoreCheckpoint { get; set; }
    public bool CanRestore => !_disposed && _lease is not null && !_recoveryRequired &&
        _state is ProfileLoadState.Loaded or ProfileLoadState.Missing or ProfileLoadState.Invalid or ProfileLoadState.Unsupported;
    public bool CanRecover => !_disposed && _lease is not null && _recoveryRequired;
    private ProfileLoadResult Result(ProfileSnapshot snapshot, ProfileLoadState state)
    {
        _state = state;
        return new(snapshot, state, FilePath);
    }
    private ProfileLoadResult LoadRecovery()
    {
        _recoveryRequired = true; _writable = false;
        ProfileSnapshot snapshot;
        try { snapshot = Recovery.ReadPrevious(); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException or
            System.Text.Json.JsonException or InvalidDataException or ArgumentException or FormatException or InvalidOperationException)
        {
            System.Diagnostics.Debug.WriteLine(error);
            snapshot = ProfileSnapshot.Defaults();
        }
        return Result(snapshot, ProfileLoadState.RecoveryRequired);
    }
    public ProfileRestoreResult Restore(ProfileSnapshot previous, ProfileSnapshot candidate, Action<ProfileSnapshot> publish)
    {
        if (!CanRestore) return new(Result(previous, _state), "현재는 데이터를 복원할 수 없습니다.");
        var oldState = _state; var oldBytes = _expectedBytes; var replaced = false; var secured = false;
        try
        {
            RestoreCheckpoint?.Invoke(RestoreStage.SerializeCandidate);
            var bytes = ProfileJson.Serialize(candidate);
            VerifyUnchanged();
            RestoreCheckpoint?.Invoke(RestoreStage.SecurePrevious);
            Recovery.Secure(previous, oldState, oldBytes); secured = true;
            _recoveryRequired = true; _writable = false;
            RestoreCheckpoint?.Invoke(RestoreStage.CandidateWrite);
            WriteProfile(bytes); replaced = true;
            RestoreCheckpoint?.Invoke(RestoreStage.PublishCandidate);
            publish(candidate);
            RestoreCheckpoint?.Invoke(RestoreStage.Complete);
            Recovery.Complete();
            _recoveryRequired = false; _writable = true;
            return new(Result(candidate, ProfileLoadState.Loaded), null);
        }
        catch (Exception error)
        {
            System.Diagnostics.Debug.WriteLine(error);
            try
            {
                if (replaced)
                {
                    RestoreCheckpoint?.Invoke(RestoreStage.RollbackWrite);
                    if (oldBytes is not null) WriteProfile(oldBytes);
                    else { VerifyUnchanged(); File.Delete(FilePath); _expectedBytes = null; }
                    publish(previous);
                }
                if (secured) Recovery.Complete();
                _recoveryRequired = false; _writable = oldState is ProfileLoadState.Loaded or ProfileLoadState.Missing;
                return new(Result(previous, oldState), "복원하지 못했습니다. 복원 전 데이터와 상태를 유지합니다.");
            }
            catch (Exception rollbackError)
            {
                System.Diagnostics.Debug.WriteLine(rollbackError);
                _recoveryRequired = true; _writable = false;
                // The marker was secured before the candidate could replace the file.
                return new(Result(previous, ProfileLoadState.RecoveryRequired), "복원을 완료하지 못했습니다. 데이터 보호를 위해 편집과 저장을 중지했습니다. 복원 전 상태로 되돌리기를 시도해 주세요.");
            }
        }
    }
    public ProfileRestoreResult Recover(ProfileSnapshot temporary, Action<ProfileSnapshot> publish)
    {
        if (!CanRecover) return new(Result(temporary, _state), "현재는 복구할 수 없습니다.");
        try
        {
            var source = Recovery.ReadSource();
            // Recovery is an explicit retry against the file currently on disk; never a normal save.
            try { _expectedBytes = File.ReadAllBytes(FilePath); }
            catch (FileNotFoundException) { _expectedBytes = null; }
            RestoreCheckpoint?.Invoke(RestoreStage.RecoveryWrite);
            WriteProfile(source.Bytes);
            if (!File.ReadAllBytes(FilePath).AsSpan().SequenceEqual(source.Bytes)) throw new IOException("Recovery verification failed.");
            RestoreCheckpoint?.Invoke(RestoreStage.RecoveryPublish);
            publish(source.Snapshot);
            RestoreCheckpoint?.Invoke(RestoreStage.Complete);
            Recovery.Complete();
            _recoveryRequired = false;
            _writable = source.Origin is ProfileLoadState.Loaded or ProfileLoadState.Missing;
            return new(Result(source.Snapshot, _writable ? ProfileLoadState.Loaded : source.Origin), null);
        }
        catch (Exception error)
        {
            System.Diagnostics.Debug.WriteLine(error);
            _recoveryRequired = true; _writable = false;
            return new(Result(temporary, ProfileLoadState.RecoveryRequired), "복원 전 상태로 돌아가지 못했습니다. 보관된 원본과 복구 정보는 유지됩니다. 접근 권한을 확인한 뒤 다시 시도해 주세요.");
        }
    }
    public void ExportBackup(ProfileSnapshot current, string destination)
    {
        var target = Path.GetFullPath(destination);
        // App-owned profile/recovery/cache files cannot be overwritten through an export dialog.
        if (Path.GetDirectoryName(target)!.Equals(DirectoryPath, StringComparison.OrdinalIgnoreCase) &&
            !Path.GetExtension(target).Equals(ProfileBackupFile.Extension, StringComparison.OrdinalIgnoreCase) ||
            target.StartsWith(Path.Combine(DirectoryPath, "fonts") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new IOException("앱 저장용 파일은 백업 위치로 선택할 수 없습니다.");
        AtomicProfileFile.Write(target, ProfileBackupFile.Export(current));
    }
}
internal enum RestoreStage { SerializeCandidate, SecurePrevious, CandidateWrite, PublishCandidate, RollbackWrite, RecoveryWrite, RecoveryPublish, Complete }
