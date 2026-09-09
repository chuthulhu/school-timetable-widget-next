using System.Windows.Threading;
using SchoolTimetableWidget.Core.Features.CurrentStatus;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Time;

namespace SchoolTimetableWidget.Desktop.Features.CurrentStatus;

/// <summary>
/// Owns the A9 refresh pipeline and timer. Construct and use on the owning UI dispatcher
/// thread, including RefreshNow, Stop and Dispose. Does not activate itself or any UI.
/// </summary>
public sealed class CurrentStatusHeaderRefreshLoop : IDisposable
{
    private readonly IApplicationClock _clock;
    private readonly PeriodDefinition[] _periods;
    private readonly CurrentStatusHeaderViewModel _viewModel;
    private readonly DispatcherTimer _timer;
    private bool _isRunning;
    private bool _disposed;

    /// <summary>
    /// Captures the supplied schedule for this loop's lifetime; Core validates it at refresh.
    /// Editable schedule replacement is a future composition decision.
    /// </summary>
    public CurrentStatusHeaderRefreshLoop(
        IApplicationClock clock,
        IEnumerable<PeriodDefinition> periods,
        CurrentStatusHeaderViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(periods);
        ArgumentNullException.ThrowIfNull(viewModel);
        _clock = clock;
        _periods = periods.ToArray();
        _viewModel = viewModel;
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        _timer.Tick += OnTick;
    }

    public bool IsRunning
    {
        get
        {
            _timer.Dispatcher.VerifyAccess();
            return _isRunning;
        }
    }

    /// <summary>Refreshes immediately before enabling the timer. Repeated Start is a no-op.</summary>
    public void Start()
    {
        VerifyUsable();
        if (_isRunning)
        {
            return;
        }

        // Mark activation before notifications, so an observer's repeated Start is also a no-op.
        _isRunning = true;
        try
        {
            RefreshNow();
            if (_isRunning && !_disposed)
            {
                _timer.Start();
            }
        }
        catch
        {
            Stop();
            throw;
        }
    }

    /// <summary>Stops automatic refresh. Repeated Stop, including after Dispose, is harmless.</summary>
    public void Stop()
    {
        _timer.Dispatcher.VerifyAccess();
        _isRunning = false;
        _timer.Stop();
    }

    /// <summary>
    /// Reads exactly one current snapshot, even while stopped. No elapsed-time accumulation
    /// or missed-tick replay. Caller must use this loop's dispatcher thread.
    /// </summary>
    public void RefreshNow()
    {
        VerifyUsable();
        var snapshot = _clock.GetSnapshot();
        var status = CurrentStatusResolver.Resolve(snapshot, _periods);
        var countdown = CurrentStatusCountdownCalculator.Calculate(snapshot, status);
        var text = CurrentStatusHeaderFormatter.Format(snapshot, status, countdown);
        _viewModel.Apply(text);
    }

    public void Dispose()
    {
        _timer.Dispatcher.VerifyAccess();
        if (_disposed)
        {
            return;
        }
        Stop();
        _timer.Tick -= OnTick;
        _disposed = true;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_isRunning && !_disposed)
        {
            RefreshNow();
        }
    }

    private void VerifyUsable()
    {
        _timer.Dispatcher.VerifyAccess();
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
