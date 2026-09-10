using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using SchoolTimetableWidget.Core.Features.Periods;

namespace SchoolTimetableWidget.Desktop.Features.PeriodScheduleEditing;

/// <summary>Unvalidated text Draft; editing never constructs or changes a Core interval.</summary>
public sealed class PeriodDraftRow : ObservableObject
{
    private string _startText;
    private string _endText;

    internal PeriodDraftRow(PeriodDefinition period)
    {
        PeriodNumber = period.PeriodNumber;
        _startText = period.Start.ToString("HH:mm", CultureInfo.InvariantCulture);
        _endText = period.End.ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    public int PeriodNumber { get; }
    public string StartText { get => _startText; set => SetProperty(ref _startText, value); }
    public string EndText { get => _endText; set => SetProperty(ref _endText, value); }
}
