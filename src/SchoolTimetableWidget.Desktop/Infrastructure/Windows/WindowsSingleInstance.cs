using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Threading;
using SchoolTimetableWidget.Desktop.Features.TrayLifecycle;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Windows;

/// <summary>Acquire/dispose on the same startup dispatcher thread. Kernel ownership outlives profile resources.</summary>
internal sealed class WindowsSingleInstance : IInstanceOwnership, IDisposable
{
    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activation;
    private RegisteredWaitHandle? _listener;
    private volatile bool _listening;
    private bool _disposed;
    private int _queued;
    public bool IsPrimary { get; }
    internal static NamedWaitHandleOptions ScopeOptions => new() { CurrentUserOnly = true, CurrentSessionOnly = true };

    internal static string ScopeFor(string? developmentDirectory, bool preview)
    {
        if (developmentDirectory is not null)
            return "dev-" + Path.TrimEndingDirectorySeparator(Path.GetFullPath(developmentDirectory)).ToUpperInvariant();
        return preview ? "development-preview" : "production";
    }

    public WindowsSingleInstance(string scope)
    {
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(scope)));
        var name = "SchoolTimetableWidget.Next." + key;
        // Every contender creates/opens the event first. An early secondary signal survives startup.
        _activation = new(false, EventResetMode.AutoReset, name + ".activate", ScopeOptions);
        try
        {
            _mutex = new(false, name + ".owner", ScopeOptions);
            try { IsPrimary = _mutex.WaitOne(0); }
            catch (AbandonedMutexException) { IsPrimary = true; }
        }
        catch { _mutex?.Dispose(); _activation.Dispose(); throw; }
    }

    public void SignalActivation()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _activation.Set();
    }

    public void Listen(Dispatcher dispatcher, Action activate)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!IsPrimary || _listener is not null) throw new InvalidOperationException("Only the primary may start one listener.");
        _listening = true;
        _listener = ThreadPool.RegisterWaitForSingleObject(_activation, (_, _) =>
        {
            if (!_listening || dispatcher.HasShutdownStarted || Interlocked.Exchange(ref _queued, 1) != 0) return;
            dispatcher.BeginInvoke(() =>
            {
                try { if (_listening && !dispatcher.HasShutdownStarted) activate(); }
                finally { Interlocked.Exchange(ref _queued, 0); }
            }, DispatcherPriority.Normal);
        }, null, Timeout.Infinite, false);
    }

    public void StopListening()
    {
        _listening = false;
        if (_listener is null) return;
        using var completed = new ManualResetEvent(false);
        if (_listener.Unregister(completed)) completed.WaitOne();
        _listener = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { StopListening(); }
        finally
        {
            try { _activation.Dispose(); }
            finally
            {
                try { if (IsPrimary) _mutex.ReleaseMutex(); }
                finally { _mutex.Dispose(); }
            }
        }
    }
}
