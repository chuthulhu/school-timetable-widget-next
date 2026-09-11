using System.Windows;

namespace SchoolTimetableWidget.Desktop.Features.DisplaySettings;

public partial class PresetNameWindow : Window
{
    private readonly Func<string, bool> _save;
    private readonly Func<string> _error;
    public PresetNameWindow(string title, string name, Func<string, bool> save, Func<string> error)
    {
        InitializeComponent();
        Title = title;
        NameInput.Text = name;
        _save = save;
        _error = error;
        Loaded += (_, _) => { NameInput.Focus(); NameInput.SelectAll(); };
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_save(NameInput.Text)) Close();
        else NameError.Text = _error();
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
