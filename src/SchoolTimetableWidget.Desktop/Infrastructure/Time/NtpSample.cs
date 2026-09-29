namespace SchoolTimetableWidget.Desktop.Infrastructure.Time;

internal sealed record NtpSample(TimeSpan Offset, TimeSpan Delay, DateTimeOffset UtcAtReceive, long ReceivedTimestamp)
{
    public static readonly TimeSpan MaximumDelay = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan Coherence = TimeSpan.FromMilliseconds(250);

    public static NtpSample? Select(IEnumerable<NtpSample> samples)
    {
        var ordered = samples.Where(s => s.Delay >= TimeSpan.Zero && s.Delay <= MaximumDelay)
            .OrderBy(s => s.Offset).ToArray();
        var bestStart = 0; var bestCount = 0; var tied = false;
        for (var start = 0; start < ordered.Length; start++)
        {
            var end = start;
            while (end < ordered.Length && ordered[end].Offset - ordered[start].Offset <= Coherence) end++;
            var count = end - start;
            if (count > bestCount) { bestStart = start; bestCount = count; tied = false; }
            else if (count == bestCount) tied = true;
        }
        return bestCount < 2 || tied ? null : ordered.Skip(bestStart).Take(bestCount).MinBy(s => s.Delay);
    }
}
