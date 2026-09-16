using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Windows;

/// <summary>
/// Keeps accepted content visible, grows back only to the user's preferred height,
/// and caps automatic sizing to the current monitor work area.
/// </summary>
public static class WindowContentMinimum
{
    private const int WmSettingChange = 0x001A;
    private const int WmDisplayChange = 0x007E;
    private const int WmDpiChanged = 0x02E0;
    private static readonly ConditionalWeakTable<Window, State> States = new();

    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(WindowContentMinimum),
        new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty IsOverflowHostProperty = DependencyProperty.RegisterAttached(
        "IsOverflowHost", typeof(bool), typeof(WindowContentMinimum), new PropertyMetadata(false));

    public static bool GetIsEnabled(Window window) => (bool)window.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(Window window, bool value) => window.SetValue(IsEnabledProperty, value);
    public static bool GetIsOverflowHost(ScrollViewer viewer) => (bool)viewer.GetValue(IsOverflowHostProperty);
    public static void SetIsOverflowHost(ScrollViewer viewer, bool value) => viewer.SetValue(IsOverflowHostProperty, value);

    internal static void SetWorkAreaProvider(Window window, IWindowWorkAreaProvider provider)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(provider);
        States.GetOrCreateValue(window).WorkArea = provider;
    }

    internal static double GetPreferredHeight(Window window) => States.GetOrCreateValue(window).PreferredHeight;

    private static void OnIsEnabledChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (target is not Window window)
            throw new ArgumentException("Content minimum can only be attached to a Window.", nameof(target));
        var state = States.GetOrCreateValue(window);
        if ((bool)e.NewValue)
        {
            window.SourceInitialized += OnSourceInitialized;
            window.Loaded += OnLoaded;
            window.SizeChanged += OnSizeChanged;
            window.LocationChanged += OnLocationChanged;
            window.Closed += OnClosed;
        }
        else
        {
            window.SourceInitialized -= OnSourceInitialized;
            window.Loaded -= OnLoaded;
            window.SizeChanged -= OnSizeChanged;
            window.LocationChanged -= OnLocationChanged;
            window.Closed -= OnClosed;
            if (state.Source is not null) state.Source.RemoveHook(WindowMessage);
            state.Source = null;
        }
    }

    private static void OnSourceInitialized(object? sender, EventArgs e)
    {
        var window = (Window)sender!;
        var state = States.GetOrCreateValue(window);
        state.Source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
        state.Source?.AddHook(WindowMessage);
    }

    private static IntPtr WindowMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message is (WmDpiChanged or WmDisplayChange or WmSettingChange) &&
            HwndSource.FromHwnd(hwnd)?.RootVisual is Window window)
            window.Dispatcher.BeginInvoke(() => Refresh(window), DispatcherPriority.Loaded);
        return IntPtr.Zero;
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        var window = (Window)sender;
        EnsurePreferredHeight(window, States.GetOrCreateValue(window));
        Refresh(window);
    }

    private static void OnLocationChanged(object? sender, EventArgs e)
    {
        var window = (Window)sender!;
        if (window.IsLoaded) Refresh(window);
    }

    private static void OnClosed(object? sender, EventArgs e)
    {
        var window = (Window)sender!;
        if (!States.TryGetValue(window, out var state)) return;
        if (state.Source is not null) state.Source.RemoveHook(WindowMessage);
        state.Source = null;
        state.Pending?.Abort();
        state.Pending = null;
    }

    private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var window = (Window)sender;
        var state = States.GetOrCreateValue(window);
        if (e.HeightChanged && !state.Applying &&
            (!double.IsFinite(state.AppliedHeight) || Math.Abs(e.NewSize.Height - state.AppliedHeight) > 0.5))
            state.PreferredHeight = e.NewSize.Height;
        if (e.WidthChanged && window.IsLoaded) Refresh(window);
    }

    public static void Refresh(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        window.Dispatcher.VerifyAccess();
        var state = States.GetOrCreateValue(window);
        if (state.Pending is { Status: DispatcherOperationStatus.Pending or DispatcherOperationStatus.Executing }) return;
        state.Pending = window.Dispatcher.BeginInvoke(() =>
        {
            state.Pending = null;
            Apply(window, state);
        }, DispatcherPriority.Loaded);
    }

    private static void Apply(Window window, State state)
    {
        if (state.Refreshing || window.Content is not FrameworkElement content || content.ActualWidth <= 0) return;
        EnsurePreferredHeight(window, state);
        state.Refreshing = true;
        try
        {
            var chromeWidth = Math.Max(0, window.ActualWidth - content.ActualWidth);
            var chromeHeight = Math.Max(0, window.ActualHeight - content.ActualHeight);
            var minimumWidth = content.MinWidth;
            content.Measure(new Size(Math.Max(minimumWidth, content.ActualWidth), double.PositiveInfinity));
            var requiredClientHeight = content.DesiredSize.Height;
            foreach (var host in Descendants<ScrollViewer>(content).Where(GetIsOverflowHost))
            {
                if (host.Content is not FrameworkElement overflowContent || host.ActualHeight <= 0) continue;
                var width = host.ViewportWidth > 0 ? host.ViewportWidth : host.ActualWidth;
                overflowContent.Measure(new Size(Math.Max(0, width), double.PositiveInfinity));
                var fixedHeight = Math.Max(0, content.ActualHeight - host.ActualHeight);
                requiredClientHeight = Math.Max(requiredClientHeight, fixedHeight + overflowContent.DesiredSize.Height);
            }
            var requiredHeight = requiredClientHeight + chromeHeight;
            var workAreaHeight = state.WorkArea.GetAvailableHeight(window);
            if (!double.IsFinite(workAreaHeight) || workAreaHeight <= 0) workAreaHeight = double.PositiveInfinity;
            var minimumHeight = Math.Min(requiredHeight, workAreaHeight);
            var targetHeight = Math.Min(Math.Max(state.PreferredHeight, requiredHeight), workAreaHeight);

            state.Applying = true;
            try
            {
                window.SetCurrentValue(Window.MinWidthProperty, minimumWidth + chromeWidth);
                if (workAreaHeight > window.MaxHeight)
                {
                    window.SetCurrentValue(Window.MaxHeightProperty, workAreaHeight);
                    window.SetCurrentValue(Window.MinHeightProperty, minimumHeight);
                }
                else
                {
                    window.SetCurrentValue(Window.MinHeightProperty, minimumHeight);
                    window.SetCurrentValue(Window.MaxHeightProperty, workAreaHeight);
                }
                if (double.IsFinite(targetHeight) && targetHeight > 0)
                {
                    state.AppliedHeight = targetHeight;
                    window.SetCurrentValue(Window.HeightProperty, targetHeight);
                    var topAdjustment = state.WorkArea.GetTopAdjustment(window, targetHeight);
                    if (double.IsFinite(topAdjustment) && Math.Abs(topAdjustment) > 0.01)
                        window.SetCurrentValue(Window.TopProperty, window.Top + topAdjustment);
                }
            }
            finally { state.Applying = false; }

            // Restore the finite-height star/scroll layout after measuring its full content.
            content.InvalidateMeasure();
        }
        finally { state.Refreshing = false; }
    }

    private static void EnsurePreferredHeight(Window window, State state)
    {
        if (double.IsFinite(state.PreferredHeight) && state.PreferredHeight > 0) return;
        state.PreferredHeight = window.ActualHeight > 0 ? window.ActualHeight : window.Height;
        if (!double.IsFinite(state.PreferredHeight) || state.PreferredHeight <= 0)
            state.PreferredHeight = 600;
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }

    private sealed class State
    {
        public IWindowWorkAreaProvider WorkArea { get; set; } = MonitorWindowWorkAreaProvider.Instance;
        public HwndSource? Source { get; set; }
        public double PreferredHeight { get; set; } = double.NaN;
        public double AppliedHeight { get; set; } = double.NaN;
        public bool Applying { get; set; }
        public bool Refreshing { get; set; }
        public DispatcherOperation? Pending { get; set; }
    }
}
