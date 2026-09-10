using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SchoolTimetableWidget.Desktop.Features.Timetable;

/// <summary>Keyboard/mouse entry and focus only; current-period highlight remains in the content.</summary>
public sealed class TimetableCellControl : ContentControl
{
    public static readonly RoutedEvent EditRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(EditRequested), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TimetableCellControl));

    public event RoutedEventHandler EditRequested
    {
        add => AddHandler(EditRequestedEvent, value);
        remove => RemoveHandler(EditRequestedEvent, value);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();
        e.Handled = true;
        if (e.ClickCount == 2) RaiseEvent(new RoutedEventArgs(EditRequestedEvent, this));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key != Key.F2) return;
        e.Handled = true;
        RaiseEvent(new RoutedEventArgs(EditRequestedEvent, this));
    }
}
