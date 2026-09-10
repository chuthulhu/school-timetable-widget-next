namespace SchoolTimetableWidget.Core.Features.Timetable;

/// <summary>Canonical immutable editable value, independent of slot or date identity.</summary>
public sealed record TimetableCellValue
{
    public TimetableCellValue(string subjectText, string classText)
    {
        ArgumentNullException.ThrowIfNull(subjectText);
        ArgumentNullException.ThrowIfNull(classText);
        SubjectText = subjectText;
        ClassText = classText;
    }

    public string SubjectText { get; }
    public string ClassText { get; }
}
