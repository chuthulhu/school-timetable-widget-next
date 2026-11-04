using SchoolTimetableWidget.Desktop.Features.Updates;

namespace SchoolTimetableWidget.Tests.Updates;

// ADR 0032 update policy against a fake source; no network, installer or timer is used.
public class UpdateCoordinatorTests
{
    [Fact]
    public async Task NotInstalledNeverChecksOrSchedules()
    {
        var source = new FakeSource { Installed = false };
        using var updates = new UpdateCoordinator(source, TimeProvider.System, null);
        updates.Start();
        Assert.Equal(UpdateCheckOutcome.NotInstalled, (await updates.CheckAsync()).Outcome);
        Assert.Equal(0, source.Downloads);
        Assert.False(updates.ApplyAfterExit(restart: true));
        Assert.Equal(0, source.Applies);
    }

    [Fact]
    public async Task CurrentVersionReportsUpToDateWithoutReadyEvent()
    {
        var source = new FakeSource();
        using var updates = new UpdateCoordinator(source, TimeProvider.System, null);
        var ready = new List<string>(); updates.Ready += ready.Add;
        Assert.Equal(UpdateCheckOutcome.UpToDate, (await updates.CheckAsync()).Outcome);
        Assert.Empty(ready); Assert.Null(updates.ReadyVersion);
    }

    [Fact]
    public async Task NewReleaseIsAnnouncedOnceAndAppliedOnlyOnRequest()
    {
        var source = new FakeSource { Latest = "0.2.0" };
        using var updates = new UpdateCoordinator(source, TimeProvider.System, null);
        var ready = new List<string>(); updates.Ready += ready.Add;
        Assert.Equal(new UpdateCheckResult(UpdateCheckOutcome.Ready, "0.2.0"), await updates.CheckAsync());
        await updates.CheckAsync();
        Assert.Equal(new[] { "0.2.0" }, ready);
        Assert.Equal(0, source.Applies);
        Assert.True(updates.ApplyAfterExit(restart: true));
        Assert.Equal((1, true), (source.Applies, source.LastRestart));
    }

    [Fact]
    public async Task FailureIsReportedAndTheNextCheckCanRunAgain()
    {
        var source = new FakeSource { Failure = new HttpRequestException("offline") };
        using var updates = new UpdateCoordinator(source, TimeProvider.System, null);
        var result = await updates.CheckAsync();
        Assert.Equal(UpdateCheckOutcome.Failed, result.Outcome);
        Assert.Contains("offline", UpdateCoordinator.Describe(result));
        source.Failure = null; source.Latest = "0.2.0";
        Assert.Equal(UpdateCheckOutcome.Ready, (await updates.CheckAsync()).Outcome);
    }

    [Fact]
    public async Task OverlappingChecksRunOnlyOneDownload()
    {
        var gate = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = new FakeSource { Pending = gate.Task };
        using var updates = new UpdateCoordinator(source, TimeProvider.System, null);
        var first = updates.CheckAsync();
        Assert.Equal(UpdateCheckOutcome.Busy, (await updates.CheckAsync()).Outcome);
        gate.SetResult(null);
        Assert.Equal(UpdateCheckOutcome.UpToDate, (await first).Outcome);
        Assert.Equal(1, source.Downloads);
    }

    [Fact]
    public void PolicyChecksSoonAfterStartThenEverySixHours()
    {
        Assert.Equal(TimeSpan.FromMinutes(1), UpdateCoordinator.FirstCheckDelay);
        Assert.Equal(TimeSpan.FromHours(6), UpdateCoordinator.CheckInterval);
    }

    private sealed class FakeSource : IUpdateSource
    {
        public bool Installed = true;
        public string? Latest;
        public Exception? Failure;
        public Task<string?>? Pending;
        public int Downloads, Applies;
        public bool LastRestart;
        public bool IsInstalled => Installed;
        public Task<string?> DownloadLatestAsync(CancellationToken token)
        {
            Downloads++;
            if (Pending is not null) return Pending;
            return Failure is null ? Task.FromResult(Latest) : Task.FromException<string?>(Failure);
        }
        public void ApplyAfterExit(bool restart) { Applies++; LastRestart = restart; }
    }
}
