using System;
using System.Globalization;

namespace BenScr.Ranges;

/// <summary>A nonempty, closed interval of signed 64-bit integers, including both endpoints.</summary>
/// <remarks>Immutable and safe for concurrent reads. The default value is the singleton [0, 0].
/// Empty sets are represented by <see cref="RangeSet.Empty"/>, never by reversed endpoints.</remarks>
public readonly struct Int64Range : IEquatable<Int64Range>, IComparable<Int64Range>
{
    /// <summary>Creates the inclusive interval [start, end].</summary>
    /// <param name="start">Inclusive lower endpoint.</param>
    /// <param name="end">Inclusive upper endpoint, at least start.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="end"/> is less than <paramref name="start"/>.</exception>
    public Int64Range(long start, long end)
    {
        if (end < start)
        {
            throw new ArgumentOutOfRangeException(nameof(end), "End must be at least start.");
        }
        Start = start;
        End = end;
    }

    /// <summary>Gets the inclusive lower endpoint.</summary>
    public long Start { get; }

    /// <summary>Gets the inclusive upper endpoint.</summary>
    public long End { get; }

    /// <summary>Gets the exact number of integers, from one through 2^64 inclusive.</summary>
    public UInt128 Cardinality => (UInt128)((Int128)End - Start + 1);

    /// <summary>Tests whether an integer is inside the inclusive interval.</summary>
    public bool Contains(long value) => value >= Start && value <= End;

    /// <summary>Tests whether every integer in another interval is contained in this interval.</summary>
    public bool Contains(Int64Range other) => other.Start >= Start && other.End <= End;

    /// <summary>Tests whether the two intervals share at least one integer.</summary>
    public bool Overlaps(Int64Range other) => Start <= other.End && other.Start <= End;

    /// <summary>Orders by lower endpoint, then by upper endpoint.</summary>
    public int CompareTo(Int64Range other)
    {
        int comparison = Start.CompareTo(other.Start);
        return comparison != 0 ? comparison : End.CompareTo(other.End);
    }

    /// <summary>Tests equality of both endpoints.</summary>
    public bool Equals(Int64Range other) => Start == other.Start && End == other.End;

    /// <summary>Tests equality when the object is an Int64Range.</summary>
    public override bool Equals(object? obj) => obj is Int64Range other && Equals(other);

    /// <summary>Returns an in-process hash of both endpoints.</summary>
    public override int GetHashCode() => HashCode.Combine(Start, End);

    /// <summary>Formats invariant decimal endpoints as [start, end].</summary>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"[{Start}, {End}]");

    /// <summary>Tests equality of both endpoints.</summary>
    public static bool operator ==(Int64Range left, Int64Range right) => left.Equals(right);

    /// <summary>Tests inequality of endpoints.</summary>
    public static bool operator !=(Int64Range left, Int64Range right) => !left.Equals(right);
}
