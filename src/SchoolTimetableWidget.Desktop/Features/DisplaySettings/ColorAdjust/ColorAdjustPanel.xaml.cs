using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SchoolTimetableWidget.Desktop.Features.DisplaySettings;

/// <summary>View of <see cref="ColorAdjustModel"/>. It owns pointer and keyboard input only.</summary>
public partial class ColorAdjustPanel : UserControl
{
    private const double SquareStep = 0.02, HueStep = 3;
    private ColorAdjustModel? _model;

    public ColorAdjustPanel()
    {
        InitializeComponent();
        DataContextChanged += (_, e) => Attach(e.NewValue as ColorAdjustModel);
    }

    /// <summary>Raised after 선택, 취소 or Esc so the host can close the popup.</summary>
    public event EventHandler? Finished;
    public ColorAdjustModel? Model => _model;

    private void Attach(ColorAdjustModel? model)
    {
        if (_model is not null) _model.PropertyChanged -= ModelChanged;
        _model = model;
        if (_model is not null) _model.PropertyChanged += ModelChanged;
        UpdateMarkers();
    }
    private void ModelChanged(object? sender, PropertyChangedEventArgs e) => UpdateMarkers();
    private void UpdateMarkers()
    {
        if (_model is null) return;
        Canvas.SetLeft(PadMarker, _model.Saturation * SaturationValuePad.Width - PadMarker.Width / 2);
        Canvas.SetTop(PadMarker, (1 - _model.Value) * SaturationValuePad.Height - PadMarker.Height / 2);
        Canvas.SetLeft(HueMarker, _model.Hue / 360 * HueBar.Width - HueMarker.Width / 2);
    }

    private void Pad_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        SaturationValuePad.Focus();
        SaturationValuePad.CaptureMouse();
        PadAt(e.GetPosition(SaturationValuePad));
        e.Handled = true;
    }
    private void Pad_MouseMove(object sender, MouseEventArgs e)
    {
        if (SaturationValuePad.IsMouseCaptured) PadAt(e.GetPosition(SaturationValuePad));
    }
    private void Hue_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        HueBar.Focus();
        HueBar.CaptureMouse();
        HueAt(e.GetPosition(HueBar));
        e.Handled = true;
    }
    private void Hue_MouseMove(object sender, MouseEventArgs e)
    {
        if (HueBar.IsMouseCaptured) HueAt(e.GetPosition(HueBar));
    }
    private void Drag_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) => ((UIElement)sender).ReleaseMouseCapture();

    internal void PadAt(Point point) =>
        _model?.SetSaturationValue(point.X / SaturationValuePad.Width, 1 - point.Y / SaturationValuePad.Height);
    internal void HueAt(Point point) => _model?.SetHue(Math.Clamp(point.X / HueBar.Width, 0, 1) * 359.9);

    private void Pad_KeyDown(object sender, KeyEventArgs e)
    {
        if (_model is null) return;
        (double ds, double dv) = e.Key switch
        {
            Key.Left => (-SquareStep, 0d), Key.Right => (SquareStep, 0d),
            Key.Up => (0d, SquareStep), Key.Down => (0d, -SquareStep), _ => (0d, 0d)
        };
        if (ds == 0 && dv == 0) return;
        _model.SetSaturationValue(_model.Saturation + ds, _model.Value + dv);
        e.Handled = true;
    }
    private void Hue_KeyDown(object sender, KeyEventArgs e)
    {
        if (_model is null) return;
        var delta = e.Key switch { Key.Left or Key.Down => -HueStep, Key.Right or Key.Up => HueStep, _ => 0d };
        if (delta == 0) return;
        _model.SetHue(_model.Hue + delta);
        e.Handled = true;
    }

    private void Choice_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ColorChoice choice }) _model?.Pick(choice.Color);
    }
    private void Confirm_Click(object sender, RoutedEventArgs e) => Finish(confirm: true);
    private void Cancel_Click(object sender, RoutedEventArgs e) => Finish(confirm: false);
    private void Panel_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Esc belongs to this window first, so the settings window's 취소 is not triggered.
        if (e.Key != Key.Escape) return;
        Finish(confirm: false);
        e.Handled = true;
    }
    internal void Finish(bool confirm)
    {
        if (_model is null || _model.IsFinished) return;
        if (confirm) _model.Confirm(); else _model.Cancel();
        Finished?.Invoke(this, EventArgs.Empty);
    }
}
