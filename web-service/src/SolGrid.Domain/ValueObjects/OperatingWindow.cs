/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: OperatingWindow.cs
 * Description: Represents one weekday availability window in a solar station schedule.
 * Contributor: Kavishi Godage
 */

namespace SolGrid.Domain.ValueObjects;

public sealed record OperatingWindow
{
    private OperatingWindow(DayOfWeek day, TimeOnly opensAt, TimeOnly closesAt)
    {
        // Store an already validated weekday availability window.
        Day = day;
        OpensAt = opensAt;
        ClosesAt = closesAt;
    }

    public DayOfWeek Day { get; }

    public TimeOnly OpensAt { get; }

    public TimeOnly ClosesAt { get; }

    public TimeSpan Duration => ClosesAt - OpensAt;

    public static OperatingWindow Create(DayOfWeek day, TimeOnly opensAt, TimeOnly closesAt)
    {
        // Reject windows that do not describe a usable slice of a single day.
        if (!Enum.IsDefined(day))
        {
            throw new ArgumentOutOfRangeException(nameof(day), day, "Day of week is not supported.");
        }

        if (closesAt <= opensAt)
        {
            throw new ArgumentException("An operating window must close after it opens.", nameof(closesAt));
        }

        return new OperatingWindow(day, opensAt, closesAt);
    }

    public bool Covers(TimeOnly time)
    {
        // Treat the closing time as exclusive so back-to-back windows do not double count.
        return time >= OpensAt && time < ClosesAt;
    }

    public bool Overlaps(OperatingWindow other)
    {
        // Detect conflicting windows configured for the same weekday.
        ArgumentNullException.ThrowIfNull(other);

        return Day == other.Day
            && OpensAt < other.ClosesAt
            && other.OpensAt < ClosesAt;
    }
}
