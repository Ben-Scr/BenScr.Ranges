using System;
using System.Collections;
using System.Collections.Generic;

namespace BenScr.Ranges;

/// <summary>An immutable set of Int64 values stored as sorted, disjoint, nonadjacent inclusive intervals.</summary>
/// <remarks>Enumeration visits intervals, not individual integers. Inputs are copied; returned sets
/// may safely share immutable storage. All instance operations support concurrent readers. Construction
/// enumerates a finite input once; its iterator remains responsible for its own synchronization and
/// progress. Iterator exceptions propagate. Memory allocation can fail normally for very large inputs.</remarks>
public sealed class RangeSet : IReadOnlyList<Int64Range>, IEquatable<RangeSet>
{
    private readonly Int64Range[] ranges;

    /// <summary>Creates a normalized set by sorting and merging overlaps and adjacent intervals.</summary>
    /// <param name="ranges">Finite input sequence; repeated or unsorted intervals are allowed.</param>
    /// <exception cref="ArgumentNullException"><paramref name="ranges"/> is null.</exception>
    /// <remarks>O(n log n) time and O(n) storage in the number of input intervals. The input is
    /// enumerated once and disposed; changing its collection during construction is unsupported.</remarks>
    public RangeSet(IEnumerable<Int64Range> ranges)
    {
        ArgumentNullException.ThrowIfNull(ranges);
        var sorted = new List<Int64Range>();
        foreach (Int64Range range in ranges)
        {
            sorted.Add(range);
        }
        sorted.Sort();
        var normalized = new List<Int64Range>(sorted.Count);
        foreach (Int64Range range in sorted)
        {
            Append(normalized, range);
        }
        this.ranges = normalized.ToArray();
        Cardinality = CountValues(this.ranges);
    }

    private RangeSet(Int64Range[] normalized)
    {
        ranges = normalized;
        Cardinality = CountValues(ranges);
    }

    /// <summary>Gets the empty set, with zero intervals and zero cardinality.</summary>
    public static RangeSet Empty { get; } = new(Array.Empty<Int64Range>());

    /// <summary>Gets all signed 64-bit integers, with cardinality 2^64.</summary>
    public static RangeSet All { get; } = new(new[] { new Int64Range(long.MinValue, long.MaxValue) });

    /// <summary>Gets the number of normalized intervals, not the number of integers.</summary>
    public int Count => ranges.Length;

    /// <summary>Gets whether the set contains no integers.</summary>
    public bool IsEmpty => ranges.Length == 0;

    /// <summary>Gets the exact number of contained integers, from zero through 2^64.</summary>
    public UInt128 Cardinality { get; }

    /// <summary>Gets a normalized interval by its zero-based position.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside [0, Count).</exception>
    public Int64Range this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, ranges.Length);
            return ranges[index];
        }
    }

    /// <summary>Creates a set containing exactly one inclusive interval.</summary>
    public static RangeSet FromRange(Int64Range range) => new(new[] { range });

    /// <summary>Tests membership in O(log n) time, where n is the interval count.</summary>
    public bool Contains(long value)
    {
        int index = FindPreceding(value);
        return index >= 0 && ranges[index].End >= value;
    }

    /// <summary>Tests whether every integer of an interval belongs to this set, in O(log n) time.</summary>
    public bool Contains(Int64Range range)
    {
        int index = FindPreceding(range.Start);
        return index >= 0 && ranges[index].End >= range.End;
    }

    /// <summary>Tests whether every integer of this set belongs to another set, in O(n + m) time.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is null.</exception>
    /// <remarks>The empty set is a subset of every set, including itself.</remarks>
    public bool IsSubsetOf(RangeSet other)
    {
        ArgumentNullException.ThrowIfNull(other);
        int index = 0;
        foreach (Int64Range range in ranges)
        {
            while (index < other.Count && other.ranges[index].End < range.Start)
            {
                index++;
            }
            if (index == other.Count || !other.ranges[index].Contains(range))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>Returns all integers in either set, merging overlap and adjacency.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is null.</exception>
    /// <remarks>O(n + m) time and output storage in interval counts; neither input is changed.</remarks>
    public RangeSet Union(RangeSet other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var result = new List<Int64Range>();
        int left = 0;
        int right = 0;
        while (left < Count || right < other.Count)
        {
            if (right == other.Count || (left < Count && ranges[left].Start <= other.ranges[right].Start))
            {
                Append(result, ranges[left++]);
            }
            else
            {
                Append(result, other.ranges[right++]);
            }
        }
        return FromNormalized(result);
    }

    /// <summary>Returns all integers shared by both sets.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is null.</exception>
    /// <remarks>O(n + m) time and output storage in interval counts; neither input is changed.</remarks>
    public RangeSet Intersect(RangeSet other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var result = new List<Int64Range>();
        int left = 0;
        int right = 0;
        while (left < Count && right < other.Count)
        {
            Int64Range first = ranges[left];
            Int64Range second = other.ranges[right];
            long start = Math.Max(first.Start, second.Start);
            long end = Math.Min(first.End, second.End);
            if (start <= end)
            {
                result.Add(new Int64Range(start, end));
            }
            if (first.End <= second.End) { left++; }
            if (second.End <= first.End) { right++; }
        }
        return FromNormalized(result);
    }

    /// <summary>Returns integers in this set that are absent from another set.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is null.</exception>
    /// <remarks>O(n + m) time and output storage in interval counts. An interval may split into
    /// several intervals. Endpoint arithmetic remains valid at Int64.MinValue and Int64.MaxValue.</remarks>
    public RangeSet Except(RangeSet other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var result = new List<Int64Range>();
        int index = 0;
        foreach (Int64Range range in ranges)
        {
            long start = range.Start;
            while (index < other.Count && other.ranges[index].End < start)
            {
                index++;
            }
            bool consumed = false;
            while (index < other.Count && other.ranges[index].Start <= range.End)
            {
                Int64Range cut = other.ranges[index];
                if (cut.Start > start)
                {
                    result.Add(new Int64Range(start, cut.Start - 1));
                }
                if (cut.End >= range.End)
                {
                    consumed = true;
                    break;
                }
                // cut.End < range.End proves that incrementing cannot overflow.
                start = cut.End + 1;
                index++;
            }
            if (!consumed)
            {
                result.Add(new Int64Range(start, range.End));
            }
        }
        return FromNormalized(result);
    }

    /// <summary>Tests equality of contained integers, independent of original interval partitioning.</summary>
    public bool Equals(RangeSet? other)
    {
        if (other is null || Count != other.Count) { return false; }
        for (int index = 0; index < Count; index++)
        {
            if (ranges[index] != other.ranges[index]) { return false; }
        }
        return true;
    }

    /// <summary>Tests value equality when the object is a RangeSet.</summary>
    public override bool Equals(object? obj) => obj is RangeSet other && Equals(other);

    /// <summary>Returns an in-process hash of normalized intervals; it is not a persistent identifier.</summary>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (Int64Range range in ranges) { hash.Add(range); }
        return hash.ToHashCode();
    }

    /// <summary>Enumerates immutable interval values in ascending order, never individual integers.</summary>
    /// <remarks>Separate enumerators may be used concurrently; each enumerator requires its own synchronization.</remarks>
    public IEnumerator<Int64Range> GetEnumerator() => ((IEnumerable<Int64Range>)ranges).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private int FindPreceding(long value)
    {
        int lower = 0;
        int upper = ranges.Length;
        while (lower < upper)
        {
            int middle = lower + (upper - lower) / 2;
            if (ranges[middle].Start <= value) { lower = middle + 1; }
            else { upper = middle; }
        }
        return lower - 1;
    }

    private static RangeSet FromNormalized(List<Int64Range> ranges) =>
        ranges.Count == 0 ? Empty : new RangeSet(ranges.ToArray());

    private static UInt128 CountValues(Int64Range[] ranges)
    {
        UInt128 count = 0;
        foreach (Int64Range range in ranges) { count += range.Cardinality; }
        return count;
    }

    private static void Append(List<Int64Range> result, Int64Range next)
    {
        if (result.Count == 0)
        {
            result.Add(next);
            return;
        }
        Int64Range previous = result[^1];
        // The guard proves +1 is valid; MaxValue already contains every later start.
        if (previous.End == long.MaxValue || next.Start <= previous.End + 1)
        {
            result[^1] = new Int64Range(previous.Start, Math.Max(previous.End, next.End));
        }
        else
        {
            result.Add(next);
        }
    }
}
