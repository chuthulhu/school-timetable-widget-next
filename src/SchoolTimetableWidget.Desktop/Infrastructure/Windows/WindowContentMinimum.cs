using System.Windows;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Windows;

/// <summary>
/// Measures the immutable startup content at the available width. Window chrome stays
/// in the Desktop boundary. No persisted preferred size or OS settings are changed.
/// </summary>
public static class WindowContentMinimum
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(WindowContentMinimum),
        new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(Window window) => (bool)window.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(Window window, bool value) => window.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (target is not Window window)
            throw new ArgumentException("Content minimum can only be attached to a Window.", nameof(target));
        if ((bool)e.NewValue)
        {
            window.Loaded += OnLoaded;
            window.SizeChanged += OnSizeChanged;
        }
        else
        {
            window.Loaded -= OnLoaded;
            window.SizeChanged -= OnSizeChanged;
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs e) => Update((Window)sender);

    private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Header text updates do not trigger this path. Height-only resizing is
        // already constrained by the most recently measured MinHeight.
        if (e.WidthChanged && ((Window)sender).IsLoaded) Update((Window)sender);
    }

    private static void Update(Window window)
    {
        if (window.Content is not FrameworkElement content || content.ActualWidth <= 0) return;
        var chromeWidth = Math.Max(0, window.ActualWidth - content.ActualWidth);
        var chromeHeight = Math.Max(0, window.ActualHeight - content.ActualHeight);
        var availableWidth = content.ActualWidth;
        var minimumWidth = content.MinWidth;
        content.Measure(new Size(Math.Max(minimumWidth, availableWidth), double.PositiveInfinity));
        var minimumHeight = content.DesiredSize.Height;
        window.SetCurrentValue(Window.MinWidthProperty, minimumWidth + chromeWidth);
        window.SetCurrentValue(Window.MinHeightProperty, minimumHeight + chromeHeight);
        // Restore normal star-row layout after the unbounded-height measurement.
        content.InvalidateMeasure();
    }
}
