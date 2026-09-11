using System.Windows.Threading;
using SchoolTimetableWidget.Core.Features.SchoolDays;
using SchoolTimetableWidget.Desktop.Features.Timetable;
using SchoolTimetableWidget.Core.Features.CurrentStatus;
using SchoolTimetableWidget.Core.Features.Periods;
using SchoolTimetableWidget.Core.Time;

namespace SchoolTimetableWidget.Desktop.Features.CurrentStatus;

/// <summary>
/// Owns the A9 refresh pipeline and timer. Construct and use on the owning UI dispatcher
/// thread, including RefreshNow, Stop and Dispose. Does not activate itself or any UI.
/// </summary>
public sealed class CurrentStatusRefreshLoop : IDisposable
{
    private readonly IApplicationClock _clock;
    private readonly Func<IReadOnlyList<PeriodDefinition>> _getSchedule;
    private readonly CurrentStatusHeaderViewModel _viewModel;
    private readonly WeeklyTimetableViewModel _timetableViewModel;
    private readonly DispatcherTimer _timer;
    private readonly Func<DateOnly, EffectiveDayConfiguration>? _resolveDay;
    private readonly Func<bool>? _getLunch;
    public DateOnly? CurrentDate { get; private set; }
    public EffectiveDayConfiguration? CurrentConfiguration { get; private set; }
    private bool _refreshing;
    private bool _isRunning;
    private bool _disposed;

    /// <summary>Fixed-snapshot convenience for callers without editable schedule ownership.</summary>
    public CurrentStatusRefreshLoop(
        IApplicationClock clock,
        IEnumerable<PeriodDefinition> periods,
        CurrentStatusHeaderViewModel viewModel,
        WeeklyTimetableViewModel timetableViewModel)
        : this(clock, CaptureSchedule(periods), viewModel, timetableViewModel) { }

    /// <summary>The source returns one stable schedule snapshot for each refresh cycle.</summary>
    public CurrentStatusRefreshLoop(
        IApplicationClock clock,
        Func<IReadOnlyList<PeriodDefinition>> getSchedule,
        CurrentStatusHeaderViewModel viewModel,
        WeeklyTimetableViewModel timetableViewModel)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(getSchedule);
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(timetableViewModel);
        _clock = clock;
        _getSchedule = getSchedule;
        _viewModel = viewModel;
        _timetableViewModel = timetableViewModel;
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        _timer.Tick += OnTick;
    }

    public CurrentStatusRefreshLoop(IApplicationClock clock,
        Func<DateOnly, EffectiveDayConfiguration> resolveDay,
        CurrentStatusHeaderViewModel viewModel, WeeklyTimetableViewModel timetableViewModel,
        Func<bool> getLunch)
        : this(clock, DefaultPeriodSchedule.Periods, viewModel, timetableViewModel)
    {
        ArgumentNullException.ThrowIfNull(resolveDay);
        ArgumentNullException.ThrowIfNull(getLunch);
        _resolveDay = resolveDay;
        _getLunch = getLunch;
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
        if (_refreshing) return;
        _refreshing = true;
        try
        {
            var snapshot = _clock.GetSnapshot();
            var effective = _resolveDay is null ? null : _resolveDay(snapshot.Date)
                    ?? throw new InvalidOperationException("Effective configuration is required.");
            if (effective is not null && effective.Date != snapshot.Date)
                throw new InvalidOperationException("Effective configuration has a different date.");
            var schedule = effective?.Schedule.Periods ?? _getSchedule();
            var status = CurrentStatusResolver.Resolve(snapshot, schedule);
            var countdown = CurrentStatusCountdownCalculator.Calculate(snapshot, status);
            var text = CurrentStatusHeaderFormatter.Format(snapshot, status, countdown, schedule, _getLunch?.Invoke() ?? false);
            var slot = CurrentTimetableSlot.From(snapshot, status);
            // Captured results are published synchronously on the same dispatcher.
            // There is no await, second clock read, or independent highlight timer.
            CurrentDate = snapshot.Date;
            CurrentConfiguration = effective;

            _viewModel.Apply(text);
            _timetableViewModel.UpdateCurrent(snapshot.Date, slot);
        }
        finally { _refreshing = false; }
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

    private static Func<IReadOnlyList<PeriodDefinition>> CaptureSchedule(IEnumerable<PeriodDefinition> periods)
    {
        ArgumentNullException.ThrowIfNull(periods);
        var captured = Array.AsReadOnly(periods.ToArray());
        return () => captured;
    }

    private void VerifyUsable()
    {
        _timer.Dispatcher.VerifyAccess();
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
