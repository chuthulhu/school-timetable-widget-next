using System.Windows;
using SchoolTimetableWidget.Desktop.Features.CurrentStatus;

namespace SchoolTimetableWidget.Desktop;

/// <summary>Composes feature views without owning their calculation or timer lifecycle.</summary>
public partial class MainWindow : Window
{
    public MainWindow(CurrentStatusHeaderViewModel headerViewModel)
    {
        ArgumentNullException.ThrowIfNull(headerViewModel);
        InitializeComponent();
        StatusHeader.DataContext = headerViewModel;
    }
}
