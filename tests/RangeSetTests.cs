using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BenScr.Ranges.Tests;

public sealed class RangeSetTests
{
    [Fact]
    public void UnsortedDuplicatesOverlapAndAdjacencyNormalize()
    {
        var set = new RangeSet(new[] { R(10, 12), R(3, 5), R(1, 3), R(10, 12), R(6, 6), R(20, 20), R(11, 15) });
        Assert.Equal(new[] { R(1, 6), R(10, 15), R(20, 20) }, set.ToArray());
        Assert.Equal(3, set.Count);
        Assert.Equal((UInt128)13, set.Cardinality);
        Assert.False(set.IsEmpty);
    }

    [Fact]
    public void EmptyAndFullDomainAreDistinct()
    {
        var empty = new RangeSet(Array.Empty<Int64Range>());
        Assert.True(empty.IsEmpty);
        Assert.Equal((UInt128)0, empty.Cardinality);
        Assert.Equal(RangeSet.Empty, empty);
        Assert.Empty(empty);
        Assert.Single(RangeSet.All);
        Assert.Equal((UInt128)1 << 64, RangeSet.All.Cardinality);
        Assert.True(RangeSet.All.Contains(R(long.MinValue, long.MaxValue)));
        Assert.False(empty.Contains(0));
        Assert.False(empty.Contains(default(Int64Range)));
    }

    [Fact]
    public void EndpointsAndAdjacencyAtBothExtremesNormalizeSafely()
    {
        var set = new RangeSet(new[] { R(long.MaxValue, long.MaxValue), R(long.MinValue, long.MinValue),
            R(long.MaxValue - 1, long.MaxValue - 1), R(long.MinValue + 1, long.MinValue + 1) });
        Assert.Equal(new[] { R(long.MinValue, long.MinValue + 1), R(long.MaxValue - 1, long.MaxValue) }, set.ToArray());
        Assert.Equal((UInt128)4, set.Cardinality);
        Assert.Equal(RangeSet.All, new RangeSet(new[] { R(0, long.MaxValue), R(long.MinValue, -1) }));
    }

    [Fact]
    public void IndexBoundsAndBothEnumeratorsExposeIntervals()
    {
        var set = new RangeSet(new[] { R(1, 3), R(8, 9) });
        Assert.Equal(R(1, 3), set[0]);
        Assert.Equal(R(8, 9), set[1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => set[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => set[2]);
        Assert.Throws<ArgumentOutOfRangeException>(() => RangeSet.Empty[0]);
        Assert.Equal(new[] { R(1, 3), R(8, 9) }, ((IEnumerable)set).Cast<Int64Range>());
    }

    [Fact]
    public void InputsAreCopiedAndResultsStayIndependent()
    {
        Int64Range[] input = { R(1, 3) };
        var set = new RangeSet(input);
        input[0] = R(50, 100);
        var union = set.Union(RangeSet.FromRange(R(4, 5)));
        Assert.Equal(new[] { R(1, 3) }, set.ToArray());
        Assert.Equal(new[] { R(1, 5) }, union.ToArray());
        Assert.False((object)set is IList<Int64Range>);
    }

    [Fact]
    public void InputIteratorIsEnumeratedOnceAndDisposed()
    {
        int starts = 0;
        bool disposed = false;
        IEnumerable<Int64Range> Input()
        {
            starts++;
            try { yield return R(1, 2); yield return R(3, 4); }
            finally { disposed = true; }
        }
        var set = new RangeSet(Input());
        Assert.Equal(1, starts);
        Assert.True(disposed);
        Assert.Equal(new[] { R(1, 4) }, set.ToArray());
    }

    [Fact]
    public void IteratorFailurePropagatesAndDisposes()
    {
        var failure = new FormatException("Invalid input record.");
        bool disposed = false;
        IEnumerable<Int64Range> Input()
        {
            try { yield return R(1, 2); throw failure; }
            finally { disposed = true; }
        }
        Assert.Same(failure, Assert.Throws<FormatException>(() => new RangeSet(Input())));
        Assert.True(disposed);
    }

    [Fact]
    public void NullInputsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new RangeSet(null!));
        Assert.Throws<ArgumentNullException>(() => RangeSet.Empty.Union(null!));
        Assert.Throws<ArgumentNullException>(() => RangeSet.Empty.Intersect(null!));
        Assert.Throws<ArgumentNullException>(() => RangeSet.Empty.Except(null!));
        Assert.Throws<ArgumentNullException>(() => RangeSet.Empty.IsSubsetOf(null!));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(7, false)]
    [InlineData(8, true)]
    [InlineData(10, true)]
    [InlineData(11, false)]
    public void MembershipHonorsGaps(long value, bool expected) =>
        Assert.Equal(expected, new RangeSet(new[] { R(1, 3), R(8, 10) }).Contains(value));

    [Fact]
    public void IntervalContainmentCannotCrossGaps()
    {
        var set = new RangeSet(new[] { R(1, 3), R(8, 10) });
        Assert.True(set.Contains(R(1, 3)));
        Assert.True(set.Contains(R(8, 9)));
        Assert.False(set.Contains(R(3, 8)));
        Assert.False(set.Contains(R(0, 2)));
        Assert.False(set.Contains(R(9, 11)));
    }

    [Fact]
    public void UnionMergesBridgesAndHandlesEmptyInputs()
    {
        var first = new RangeSet(new[] { R(1, 3), R(8, 10) });
        var second = new RangeSet(new[] { R(4, 7), R(20, 30) });
        Assert.Equal(new[] { R(1, 10), R(20, 30) }, first.Union(second).ToArray());
        Assert.Equal(first, first.Union(RangeSet.Empty));
        Assert.Equal(first, RangeSet.Empty.Union(first));
        Assert.Equal(first, first.Union(first));
        Assert.Equal(RangeSet.All, first.Union(RangeSet.All));
    }

    [Fact]
    public void IntersectionRetainsSharedEndpointsAndHandlesEmptyInputs()
    {
        var first = new RangeSet(new[] { R(1, 3), R(8, 10) });
        var second = new RangeSet(new[] { R(3, 8), R(10, 20) });
        Assert.Equal(new[] { R(3, 3), R(8, 8), R(10, 10) }, first.Intersect(second).ToArray());
        Assert.Empty(first.Intersect(RangeSet.Empty));
        Assert.Empty(RangeSet.Empty.Intersect(first));
        Assert.Equal(first, first.Intersect(first));
        Assert.Equal(first, first.Intersect(RangeSet.All));
    }

    [Fact]
    public void DifferenceSplitsAndRemovesClosedEndpoints()
    {
        var first = RangeSet.FromRange(R(0, 20));
        var cut = new RangeSet(new[] { R(0, 0), R(3, 6), R(10, 10), R(20, 30) });
        Assert.Equal(new[] { R(1, 2), R(7, 9), R(11, 19) }, first.Except(cut).ToArray());
        Assert.Equal(first, first.Except(RangeSet.Empty));
        Assert.Empty(RangeSet.Empty.Except(first));
        Assert.Empty(first.Except(first));
        Assert.Empty(first.Except(RangeSet.All));
    }

    [Fact]
    public void DifferenceKeepsCutsSpanningMultipleInputIntervals()
    {
        var set = new RangeSet(new[] { R(-10, -5), R(0, 5), R(10, 20), R(30, 40) });
        var cut = new RangeSet(new[] { R(-8, 12), R(35, 45) });
        Assert.Equal(new[] { R(-10, -9), R(13, 20), R(30, 34) }, set.Except(cut).ToArray());
    }

    [Fact]
    public void DifferenceHandlesFullDomainAndExtremeSingletons()
    {
        var endpoints = new RangeSet(new[] { R(long.MinValue, long.MinValue), R(long.MaxValue, long.MaxValue) });
        Assert.Equal(new[] { R(long.MinValue + 1, long.MaxValue - 1) }, RangeSet.All.Except(endpoints).ToArray());
        Assert.Equal(new[] { R(long.MinValue, -1) }, RangeSet.All.Except(RangeSet.FromRange(R(0, long.MaxValue))).ToArray());
        Assert.Equal(new[] { R(0, long.MaxValue) }, RangeSet.All.Except(RangeSet.FromRange(R(long.MinValue, -1))).ToArray());
        Assert.Empty(endpoints.Except(RangeSet.All));
        Assert.Equal(endpoints, RangeSet.All.Except(RangeSet.FromRange(R(long.MinValue + 1, long.MaxValue - 1))));
    }

    [Fact]
    public void SubsetIncludesEmptyAndRequiresEveryInterval()
    {
        var set = new RangeSet(new[] { R(1, 3), R(8, 10) });
        Assert.True(RangeSet.Empty.IsSubsetOf(set));
        Assert.True(RangeSet.Empty.IsSubsetOf(RangeSet.Empty));
        Assert.True(set.IsSubsetOf(set));
        Assert.True(set.IsSubsetOf(RangeSet.All));
        Assert.False(set.IsSubsetOf(RangeSet.Empty));
        Assert.False(RangeSet.FromRange(R(2, 9)).IsSubsetOf(set));
        Assert.False(new RangeSet(new[] { R(2, 3), R(8, 11) }).IsSubsetOf(set));
        Assert.True(new RangeSet(new[] { R(1, 1), R(3, 3), R(8, 9) }).IsSubsetOf(set));
    }

    [Fact]
    public void EqualityAndHashingUseCanonicalMembership()
    {
        var first = new RangeSet(new[] { R(1, 2), R(3, 5) });
        var equal = RangeSet.FromRange(R(1, 5));
        Assert.True(first.Equals(equal));
        Assert.True(first.Equals((object)equal));
        Assert.False(first.Equals((RangeSet?)null));
        Assert.False(first.Equals("[1, 5]"));
        Assert.False(first.Equals(RangeSet.Empty));
        Assert.False(first.Equals(RangeSet.FromRange(R(1, 4))));
        Assert.Equal(first.GetHashCode(), equal.GetHashCode());
        Assert.Single(new HashSet<RangeSet> { first, equal });
    }

    [Fact]
    public async Task ConcurrentReadersAndSetOperationsAreIndependent()
    {
        var set = new RangeSet(new[] { R(-100, -50), R(0, 100) });
        await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() =>
        {
            for (int index = 0; index < 100; index++)
            {
                Assert.True(set.Contains(index));
                Assert.Equal(set, set.Intersect(RangeSet.All));
                Assert.Empty(set.Except(set));
                Assert.Equal((UInt128)152, set.Cardinality);
            }
        }, TestContext.Current.CancellationToken)));
    }

    private static Int64Range R(long start, long end) => new(start, end);
}
