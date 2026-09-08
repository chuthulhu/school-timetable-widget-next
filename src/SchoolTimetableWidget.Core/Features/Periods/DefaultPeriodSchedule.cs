namespace SchoolTimetableWidget.Core.Features.Periods;

/// <summary>The approved default profile; callers may supply their own definitions.</summary>
public static class DefaultPeriodSchedule
{
    public static IReadOnlyList<PeriodDefinition> Periods { get; } = Array.AsReadOnly<PeriodDefinition>(
    [
        new(1, new TimeOnly(9, 0), new TimeOnly(9, 50)),
        new(2, new TimeOnly(10, 0), new TimeOnly(10, 50)),
        new(3, new TimeOnly(11, 0), new TimeOnly(11, 50)),
        new(4, new TimeOnly(12, 0), new TimeOnly(12, 50)),
        new(5, new TimeOnly(14, 0), new TimeOnly(14, 50)),
        new(6, new TimeOnly(15, 0), new TimeOnly(15, 50)),
        new(7, new TimeOnly(16, 0), new TimeOnly(16, 50)),
    ]);
}
