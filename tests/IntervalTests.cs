using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace BenScr.Ranges.Tests;

public sealed class IntervalTests
{
    [Theory]
    [InlineData(0, 0, 1)]
    [InlineData(1, 3, 3)]
    [InlineData(-3, -1, 3)]
    [InlineData(-2, 2, 5)]
    [InlineData(long.MinValue, long.MinValue, 1)]
    [InlineData(long.MaxValue, long.MaxValue, 1)]
    public void ClosedCardinalityIsExact(long start, long end, ulong count)
    {
        var range = new Int64Range(start, end);
        Assert.Equal(start, range.Start);
        Assert.Equal(end, range.End);
        Assert.Equal((UInt128)count, range.Cardinality);
        Assert.True(range.Contains(start));
        Assert.True(range.Contains(end));
    }

    [Fact]
    public void FullDomainAndHalfDomainsDoNotOverflow()
    {
        Assert.Equal((UInt128)1 << 64, new Int64Range(long.MinValue, long.MaxValue).Cardinality);
        Assert.Equal((UInt128)1 << 63, new Int64Range(long.MinValue, -1).Cardinality);
        Assert.Equal((UInt128)1 << 63, new Int64Range(0, long.MaxValue).Cardinality);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(long.MaxValue, long.MinValue)]
    [InlineData(0, -1)]
    public void ReversedEndpointsAreRejected(long start, long end)
    {
        Assert.Equal("end", Assert.Throws<ArgumentOutOfRangeException>(() => new Int64Range(start, end)).ParamName);
    }

    [Fact]
    public void DefaultIsZeroSingleton()
    {
        Int64Range range = default;
        Assert.Equal(new Int64Range(0, 0), range);
        Assert.Equal((UInt128)1, range.Cardinality);
    }

    [Theory]
    [InlineData(-4, false)]
    [InlineData(-3, true)]
    [InlineData(0, true)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    public void MembershipIncludesOnlyEndpointsAndInterior(long value, bool expected) =>
        Assert.Equal(expected, new Int64Range(-3, 4).Contains(value));

    [Fact]
    public void IntervalContainmentAndOverlapAreDifferent()
    {
        var range = new Int64Range(2, 8);
        Assert.True(range.Contains(new Int64Range(2, 8)));
        Assert.True(range.Contains(new Int64Range(5, 5)));
        Assert.False(range.Contains(new Int64Range(1, 8)));
        Assert.False(range.Contains(new Int64Range(2, 9)));
        Assert.True(range.Overlaps(new Int64Range(8, 15)));
        Assert.True(range.Overlaps(new Int64Range(-10, 2)));
        Assert.False(range.Overlaps(new Int64Range(9, 10)));
        Assert.False(range.Overlaps(new Int64Range(-10, 1)));
    }

    [Fact]
    public void EqualityHashingAndLexicographicOrderingAgree()
    {
        var first = new Int64Range(1, 2);
        var equal = new Int64Range(1, 2);
        Assert.True(first == equal);
        Assert.False(first != equal);
        Assert.True(first.Equals((object)equal));
        Assert.False(first.Equals(null));
        Assert.False(first.Equals("[1, 2]"));
        Assert.Equal(first.GetHashCode(), equal.GetHashCode());
        Assert.Single(new HashSet<Int64Range> { first, equal });
        Assert.True(first.CompareTo(new Int64Range(1, 3)) < 0);
        Assert.True(first.CompareTo(new Int64Range(0, 10)) > 0);
        Assert.Equal(0, first.CompareTo(equal));
        Assert.True(new Int64Range(long.MinValue, long.MinValue).CompareTo(new Int64Range(long.MaxValue, long.MaxValue)) < 0);
    }

    [Fact]
    public void FormattingUsesInvariantEndpoints()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            var custom = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            custom.NumberFormat.NegativeSign = "NEG";
            CultureInfo.CurrentCulture = custom;
            Assert.Equal("[-1234, 5678]", new Int64Range(-1234, 5678).ToString());
        }
        finally { CultureInfo.CurrentCulture = original; }
    }
}
