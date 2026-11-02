using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Xunit;

namespace BenScr.Ranges.Tests;

public sealed class ReferenceTests
{
    [Fact]
    public void AllPairsOfEightValueSetsAgreeWithIndependentMembershipOracle()
    {
        RangeSet[] sets = Enumerable.Range(0, 256).Select(mask => new RangeSet(
            Enumerable.Range(0, 8).Where(bit => (mask & (1 << bit)) != 0).Select(bit => new Int64Range(bit - 4, bit - 4)))).ToArray();
        for (int left = 0; left < 256; left++)
        {
            for (int right = 0; right < 256; right++)
            {
                VerifyMask(sets[left].Union(sets[right]), left | right);
                VerifyMask(sets[left].Intersect(sets[right]), left & right);
                VerifyMask(sets[left].Except(sets[right]), left & ~right & 255);
                Assert.Equal((left & ~right) == 0, sets[left].IsSubsetOf(sets[right]));
            }
        }
    }

    [Fact]
    public void FixedSeedUnsortedRangesAgreeWithHashSetOracle()
    {
        var random = new System.Random(710729);
        for (int iteration = 0; iteration < 2000; iteration++)
        {
            var leftInput = Input(random);
            var rightInput = Input(random);
            var leftValues = Expand(leftInput);
            var rightValues = Expand(rightInput);
            var left = new RangeSet(leftInput);
            var right = new RangeSet(rightInput);
            CheckValues(left, leftValues);
            CheckValues(right, rightValues);
            CheckValues(left.Union(right), leftValues.Union(rightValues).ToHashSet());
            CheckValues(left.Intersect(right), leftValues.Intersect(rightValues).ToHashSet());
            CheckValues(left.Except(right), leftValues.Except(rightValues).ToHashSet());
            Assert.Equal(leftValues.IsSubsetOf(rightValues), left.IsSubsetOf(right));
            long start = random.Next(-51, 51);
            long end = random.Next((int)start, 52);
            bool contained = Enumerable.Range((int)start, (int)(end - start + 1)).All(value => leftValues.Contains(value));
            Assert.Equal(contained, left.Contains(new Int64Range(start, end)));
        }
    }

    [Fact]
    public void ExtremeAlgebraAndCardinalityAgreeWithBigInteger()
    {
        long[] points = { long.MinValue, long.MinValue + 1, long.MinValue + 2, -1, 0, 1, long.MaxValue - 2, long.MaxValue - 1, long.MaxValue };
        var inputs = new List<RangeSet> { RangeSet.Empty, RangeSet.All };
        for (int start = 0; start < points.Length; start++)
        {
            for (int end = start; end < points.Length; end++)
            {
                var range = new Int64Range(points[start], points[end]);
                Assert.Equal((BigInteger)points[end] - points[start] + 1, BigInteger.Parse(range.Cardinality.ToString()));
                inputs.Add(RangeSet.FromRange(range));
            }
        }
        foreach (RangeSet left in inputs)
        {
            foreach (RangeSet right in inputs)
            {
                RangeSet union = left.Union(right);
                RangeSet intersection = left.Intersect(right);
                RangeSet difference = left.Except(right);
                Assert.Equal(left.Cardinality + right.Cardinality, union.Cardinality + intersection.Cardinality);
                Assert.Equal(left.Cardinality, intersection.Cardinality + difference.Cardinality);
                Assert.Equal(left, intersection.Union(difference));
                Assert.Empty(difference.Intersect(right));
                VerifyNormalized(union);
                VerifyNormalized(intersection);
                VerifyNormalized(difference);
                foreach (long point in points)
                {
                    Assert.Equal(left.Contains(point) || right.Contains(point), union.Contains(point));
                    Assert.Equal(left.Contains(point) && right.Contains(point), intersection.Contains(point));
                    Assert.Equal(left.Contains(point) && !right.Contains(point), difference.Contains(point));
                }
            }
        }
    }

    [Fact]
    public void LargeDisjointInputDoesNotExpandItsValues()
    {
        var ranges = Enumerable.Range(0, 10_000).Select(index => new Int64Range((long)index * 1_000_000, (long)index * 1_000_000 + 999_998)).ToArray();
        var set = new RangeSet(ranges.Reverse());
        Assert.Equal(10_000, set.Count);
        Assert.Equal((UInt128)9_999_990_000, set.Cardinality);
        Assert.Equal(set, set.Intersect(RangeSet.All));
        Assert.Empty(set.Except(set));
        VerifyNormalized(set);
    }

    private static Int64Range[] Input(System.Random random) => Enumerable.Range(0, random.Next(0, 12))
        .Select(_ => { int first = random.Next(-50, 51); int second = random.Next(-50, 51); return new Int64Range(Math.Min(first, second), Math.Max(first, second)); }).ToArray();

    private static HashSet<long> Expand(IEnumerable<Int64Range> ranges)
    {
        var values = new HashSet<long>();
        foreach (Int64Range range in ranges)
        {
            for (long value = range.Start; value <= range.End; value++) { values.Add(value); }
        }
        return values;
    }

    private static void CheckValues(RangeSet set, HashSet<long> expected)
    {
        Assert.Equal((UInt128)expected.Count, set.Cardinality);
        Assert.True(expected.SetEquals(Expand(set)));
        for (int value = -51; value <= 51; value++) { Assert.Equal(expected.Contains(value), set.Contains(value)); }
        VerifyNormalized(set);
    }

    private static void VerifyMask(RangeSet set, int mask)
    {
        Assert.Equal((UInt128)BitOperations.PopCount((uint)mask), set.Cardinality);
        for (int bit = 0; bit < 8; bit++) { Assert.Equal((mask & (1 << bit)) != 0, set.Contains(bit - 4)); }
        Assert.False(set.Contains(-5));
        Assert.False(set.Contains(4));
        VerifyNormalized(set);
    }

    private static void VerifyNormalized(RangeSet set)
    {
        BigInteger cardinality = 0;
        for (int index = 0; index < set.Count; index++)
        {
            Int64Range range = set[index];
            Assert.True(range.Start <= range.End);
            if (index > 0) { Assert.True((BigInteger)range.Start > (BigInteger)set[index - 1].End + 1); }
            cardinality += (BigInteger)range.End - range.Start + 1;
        }
        Assert.Equal(cardinality, BigInteger.Parse(set.Cardinality.ToString()));
    }
}
