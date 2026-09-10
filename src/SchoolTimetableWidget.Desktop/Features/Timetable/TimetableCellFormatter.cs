using SchoolTimetableWidget.Core.Features.Timetable;

namespace SchoolTimetableWidget.Desktop.Features.Timetable;

/// <summary>Display projection only: never parsed back into canonical fields.</summary>
public static class TimetableCellFormatter
{
    public static string Format(TimetableCellValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.SubjectText.Length == 0) return value.ClassText;
        if (value.ClassText.Length == 0) return value.SubjectText;
        return value.SubjectText + "\n" + value.ClassText;
    }
}
