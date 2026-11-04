namespace SchoolTimetableWidget.Desktop.Features.Updates;

/// <summary>Installer-specific update operations behind a boundary (ADR 0032).</summary>
public interface IUpdateSource
{
    bool IsInstalled { get; }
    /// <summary>Downloads the newest stable release if it is newer; returns its version, or null when current.</summary>
    Task<string?> DownloadLatestAsync(CancellationToken token);
    /// <summary>Starts the updater, which applies the downloaded release once this process has exited.</summary>
    void ApplyAfterExit(bool restart);
}

public enum UpdateCheckOutcome { NotInstalled, Busy, UpToDate, Ready, Failed }
public sealed record UpdateCheckResult(UpdateCheckOutcome Outcome, string? Version = null, string? Error = null);

/// <summary>
/// Background check and download only. A downloaded release installs at the next start, or at once when
/// the user picks restart; nothing is applied silently while the widget is running.
/// </summary>
public sealed class UpdateCoordinator : IDisposable
{
    public static TimeSpan FirstCheckDelay { get; } = TimeSpan.FromMinutes(1);
    public static TimeSpan CheckInterval { get; } = TimeSpan.FromHours(6);
    private readonly IUpdateSource _source;
    private readonly TimeProvider _time;
    private readonly SynchronizationContext? _context;
    private readonly CancellationTokenSource _lifetime = new();
    private ITimer? _timer;
    private int _busy;
    public UpdateCoordinator(IUpdateSource source, TimeProvider time, SynchronizationContext? context)
    {
        _source = source; _time = time; _context = context;
    }
    /// <summary>Raised on the captured context once per newly downloaded version.</summary>
    public event Action<string>? Ready;
    public string? ReadyVersion { get; private set; }

    public void Start()
    {
        if (!_source.IsInstalled || _timer is not null) return;
        _timer = _time.CreateTimer(_ => _ = CheckAsync(), null, FirstCheckDelay, CheckInterval);
    }

    public async Task<UpdateCheckResult> CheckAsync()
    {
        if (!_source.IsInstalled) return new(UpdateCheckOutcome.NotInstalled);
        if (Interlocked.Exchange(ref _busy, 1) == 1) return new(UpdateCheckOutcome.Busy);
        try
        {
            var version = await _source.DownloadLatestAsync(_lifetime.Token).ConfigureAwait(false);
            if (version is null) return new(UpdateCheckOutcome.UpToDate);
            if (version != ReadyVersion)
            {
                ReadyVersion = version;
                if (_context is null) Ready?.Invoke(version);
                else _context.Post(_ => Ready?.Invoke(version), null);
            }
            return new(UpdateCheckOutcome.Ready, version);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { return new(UpdateCheckOutcome.Failed, Error: "종료 중입니다."); }
        catch (Exception error) { return new(UpdateCheckOutcome.Failed, Error: error.Message); }
        finally { Volatile.Write(ref _busy, 0); }
    }

    /// <summary>Hands the downloaded release to the updater; call only when the app is really exiting.</summary>
    public bool ApplyAfterExit(bool restart)
    {
        if (ReadyVersion is null) return false;
        _source.ApplyAfterExit(restart);
        return true;
    }

    public static string Describe(UpdateCheckResult result) => result.Outcome switch
    {
        UpdateCheckOutcome.NotInstalled => "설치된 앱에서만 업데이트를 확인할 수 있습니다.",
        UpdateCheckOutcome.Busy => "이미 업데이트를 확인하는 중입니다.",
        UpdateCheckOutcome.UpToDate => "최신 버전을 사용 중입니다.",
        UpdateCheckOutcome.Ready => $"새 버전 {result.Version}을(를) 받았습니다. 다음 실행 때 적용되며, 트레이 메뉴에서 바로 다시 시작할 수도 있습니다.",
        _ => $"업데이트를 확인하지 못했습니다. 인터넷 연결을 확인해 주세요. ({result.Error})"
    };

    public void Dispose()
    {
        _timer?.Dispose();
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
}
