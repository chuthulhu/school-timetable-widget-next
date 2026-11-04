using SchoolTimetableWidget.Desktop.Features.Updates;
using Velopack;
using Velopack.Sources;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Windows;

/// <summary>Stable GitHub Releases of this repository, as published by installer.yml (ADR 0032).</summary>
internal sealed class VelopackUpdateSource : IUpdateSource
{
    private const string Repository = "https://github.com/chuthulhu/school-timetable-widget-next";
    private readonly UpdateManager _manager = new(new GithubSource(Repository, null, false, null), null, null);
    private VelopackAsset? _ready;
    public bool IsInstalled => _manager.IsInstalled;

    public async Task<string?> DownloadLatestAsync(CancellationToken token)
    {
        if (_manager.UpdatePendingRestart is { } pending) { _ready = pending; return pending.Version.ToString(); }
        var info = await _manager.CheckForUpdatesAsync().ConfigureAwait(false);
        if (info is null) return null;
        await _manager.DownloadUpdatesAsync(info, null, token).ConfigureAwait(false);
        _ready = info.TargetFullRelease;
        return _ready.Version.ToString();
    }

    public void ApplyAfterExit(bool restart)
    {
        if (_ready is not null) _manager.WaitExitThenApplyUpdates(_ready, true, restart, []);
    }
}
