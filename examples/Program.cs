using System;
using System.Linq;
using BenScr.Ranges;

Int64Range[] input = { new(10, 20), new(1, 5), new(6, 9) };
var available = new RangeSet(input);
input[0] = new Int64Range(100, 200);
Require(available.Equals(RangeSet.FromRange(new Int64Range(1, 20))), "Normalized immutable copy");
Require(available.Cardinality == 20 && available.Contains(1) && available.Contains(20) && !available.Contains(21), "Closed membership");

var reserved = new RangeSet(new[] { new Int64Range(3, 6), new Int64Range(10, 15) });
RangeSet free = available.Except(reserved);
Require(free.SequenceEqual(new[] { new Int64Range(1, 2), new Int64Range(7, 9), new Int64Range(16, 20) }), "Difference splits");
Require(free.Cardinality == 10 && free.IsSubsetOf(available), "Subset and cardinality");
Require(free.Union(reserved).Equals(available), "Union reconstructs availability");
Require(free.Intersect(reserved).IsEmpty, "Intersection excludes reservations");
Require(!free.Contains(new Int64Range(2, 7)), "Containment cannot cross gaps");

Require(RangeSet.All.Cardinality == (UInt128)1 << 64, "Full signed-64 domain");
var endpoints = new RangeSet(new[] { new Int64Range(long.MinValue, long.MinValue), new Int64Range(long.MaxValue, long.MaxValue) });
RangeSet interior = RangeSet.All.Except(endpoints);
Require(interior.Equals(RangeSet.FromRange(new Int64Range(long.MinValue + 1, long.MaxValue - 1))), "Extreme subtraction");
Require(interior.Cardinality + endpoints.Cardinality == RangeSet.All.Cardinality, "Exact cardinality conservation");
Require(RangeSet.Empty.IsSubsetOf(interior), "Empty subset");
Console.WriteLine("Ranges verified: normalized intervals, set algebra, closed boundaries, exact full-domain counts.");

static void Require(bool condition, string scenario)
{
    if (!condition) { throw new InvalidOperationException(scenario); }
}
