using CommunityToolkit.Mvvm.ComponentModel;
using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Desktop.Features.DateOverrides;

public sealed class DateTimetableDraftRow(int period, TimetableCellValue value) : ObservableObject
{
    private string _subject = value.SubjectText;
    private string _class = value.ClassText;
    public int PeriodNumber { get; } = period;
    public string SubjectText { get => _subject; set => SetProperty(ref _subject, value); }
    public string ClassText { get => _class; set => SetProperty(ref _class, value); }
}
