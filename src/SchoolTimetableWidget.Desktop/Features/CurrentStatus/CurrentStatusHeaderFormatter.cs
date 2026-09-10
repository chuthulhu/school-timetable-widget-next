using System.Globalization;
using SchoolTimetableWidget.Core.Features.CurrentStatus;
using SchoolTimetableWidget.Core.Time;

namespace SchoolTimetableWidget.Desktop.Features.CurrentStatus;

/// <summary>Formats A8 Korean header text from already calculated Core facts.</summary>
public static class CurrentStatusHeaderFormatter
{
    /// <summary>
    /// The caller supplies status and countdown calculated from the same snapshot.
    /// Validates countdown presence, not date/schedule provenance. Does not read a
    /// clock, resolve status or recalculate countdown semantics.
    /// </summary>
    /// <exception cref="ArgumentException">Countdown presence does not match the status kind.</exception>
    public static CurrentStatusHeaderText Format(
        ApplicationTimeSnapshot snapshot,
        CurrentStatusResult status,
        CountdownDisplayValue? countdown)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(status);

        var requiresCountdown = status.Kind switch
        {
            CurrentStatusKind.BeforeFirstPeriod or CurrentStatusKind.InPeriod or CurrentStatusKind.Break => true,
            CurrentStatusKind.AfterLastPeriod or CurrentStatusKind.Weekend => false,
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };
        if (requiresCountdown != (countdown is not null))
        {
            throw new ArgumentException("Countdown presence must match the status kind.", nameof(countdown));
        }

        var countdownText = countdown is null ? null : FormatCountdown(countdown);
        var statusText = status.Kind switch
        {
            CurrentStatusKind.BeforeFirstPeriod =>
                FormattableString.Invariant($"{status.NextPeriodNumber}교시까지 {countdownText}"),
            CurrentStatusKind.InPeriod =>
                FormattableString.Invariant($"{status.CurrentPeriodNumber}교시 · 종료까지 {countdownText}"),
            CurrentStatusKind.Break =>
                FormattableString.Invariant($"쉬는시간 · {status.NextPeriodNumber}교시까지 {countdownText}"),
            CurrentStatusKind.AfterLastPeriod => "오늘 수업 종료",
            CurrentStatusKind.Weekend => "오늘은 수업이 없습니다",
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };

        return new CurrentStatusHeaderText(
            snapshot.Date.ToString("yyyy년 MM월 dd일", CultureInfo.InvariantCulture),
            snapshot.LocalTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture), statusText);
    }

    private static string FormatCountdown(CountdownDisplayValue countdown)
    {
        // Core guarantees positive, normalized meaning. LessThanMinute has 0/0 fields.
        if (countdown.LessThanMinute)
        {
            return "1분 미만";
        }
        if (countdown.Hours == 0)
        {
            return FormattableString.Invariant($"{countdown.Minutes}분");
        }
        if (countdown.Minutes == 0)
        {
            return FormattableString.Invariant($"{countdown.Hours}시간");
        }
        return FormattableString.Invariant($"{countdown.Hours}시간 {countdown.Minutes}분");
    }
}
