using System.Windows;
using SchoolTimetableWidget.Core.Time;
using SchoolTimetableWidget.Desktop.Infrastructure.Time;

namespace SchoolTimetableWidget.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    // Composition root owns one clock for this App's lifetime. Future consumers
    // receive this instance through constructors when assembled here.
    internal IApplicationClock ApplicationClock { get; } = new PcFallbackApplicationClock();
}
