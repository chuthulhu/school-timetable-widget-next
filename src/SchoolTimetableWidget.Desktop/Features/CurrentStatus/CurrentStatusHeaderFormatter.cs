using System.Globalization;
using SchoolTimetableWidget.Core.Features.Periods;
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
        CountdownDisplayValue? countdown,
        IReadOnlyList<PeriodDefinition>? effectiveSchedule = null,
        bool showLunch = false)
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

        var breakLabel = "쉬는시간";
        if (showLunch && status.Kind == CurrentStatusKind.Break && effectiveSchedule is not null)
        {
            var fourth = effectiveSchedule.SingleOrDefault(p => p.PeriodNumber == 4);
            var fifth = effectiveSchedule.SingleOrDefault(p => p.PeriodNumber == 5);
            if (fourth is not null && fifth is not null &&
                fourth.End <= snapshot.TimeOfDay && snapshot.TimeOfDay < fifth.Start)
                breakLabel = "점심시간";
        }
        var countdownText = countdown is null ? null : FormatCountdown(countdown);
        var statusText = status.Kind switch
        {
            CurrentStatusKind.BeforeFirstPeriod =>
                FormattableString.Invariant($"{status.NextPeriodNumber}교시까지 {countdownText}"),
            CurrentStatusKind.InPeriod =>
                FormattableString.Invariant($"{status.CurrentPeriodNumber}교시 · 종료까지 {countdownText}"),
            CurrentStatusKind.Break =>
                FormattableString.Invariant($"{breakLabel} · {status.NextPeriodNumber}교시까지 {countdownText}"),
            CurrentStatusKind.AfterLastPeriod => "오늘 수업 종료",
            CurrentStatusKind.Weekend => "오늘은 수업이 없습니다",
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };

        return new CurrentStatusHeaderText(
            snapshot.Date.ToString("yyyy년 MM월 dd일", CultureInfo.InvariantCulture),
            snapshot.LocalTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture), statusText,
            new[] { "일요일", "월요일", "화요일", "수요일", "목요일", "금요일", "토요일" }[(int)snapshot.Date.DayOfWeek],
            snapshot.LocalTime.Hour < 12 ? "오전" : "오후",
            snapshot.LocalTime.ToString("h:mm:ss", CultureInfo.InvariantCulture));
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
