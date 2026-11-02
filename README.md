# BenScr.Ranges

Immutable inclusive Int64 intervals and normalized range sets for .NET 10. Combine covered ranges,
subtract reservations, test membership, and count values exactly without enumerating each integer.
There are no runtime package dependencies.

Created by Ben Schneider — https://github.com/Ben-Scr

```shell
dotnet add package BenScr.Ranges --version 1.0.0
```

```csharp
using System;
using BenScr.Ranges;

var covered = new RangeSet(new[]
{
    new Int64Range(10, 20),
    new Int64Range(1, 9)
});
var reserved = RangeSet.FromRange(new Int64Range(4, 12));
RangeSet remaining = covered.Except(reserved);

foreach (Int64Range range in remaining)
{
    Console.WriteLine(range); // [1, 3], then [13, 20]
}
Console.WriteLine(remaining.Cardinality); // 11
Console.WriteLine(remaining.Contains(20)); // True
Console.WriteLine(RangeSet.All.Cardinality); // 18446744073709551616
```

The complete executable example in [examples/Program.cs](examples/Program.cs) checks normalization,
set operations, immutable ownership, gaps and extreme endpoints. It also serves as the isolated
NuGet consumer.

## Closed integer intervals

`Int64Range(start, end)` includes **both** endpoints and requires `start <= end`. Every interval is
nonempty. A singleton such as `[5, 5]` contains one integer; `default(Int64Range)` is `[0, 0]`.
Use `RangeSet.Empty` for no values. A reversed interval throws `ArgumentOutOfRangeException`.
This is distinct from `System.Range`, which models index ranges with an exclusive upper endpoint.

`Contains(long)` includes the endpoints. `Contains(Int64Range)` requires the entire other interval
to fit inside. `Overlaps` requires a shared integer; adjacent intervals do not overlap. Comparison
orders intervals by start, then end. Equality and hashing use both endpoints, and `ToString()` uses
invariant decimal notation `[start, end]`.

## Normalized sets

Construction sorts inputs and merges overlaps, duplicates and adjacency. For example,
`[1, 3], [4, 5], [2, 4], [10, 10]` becomes `[1, 5], [10, 10]`. Each stored interval is disjoint from
the next with at least one missing integer between them. Int64 minimum and maximum are valid;
merging and splitting avoid endpoint overflow, including in checked builds.

- `Count` counts **intervals**. `Cardinality` counts **integers**.
- Enumeration and the indexer return interval values in ascending order. They never expand a range
  into all of its integers. An invalid index throws `ArgumentOutOfRangeException`.
- `Union` returns integers in either set; `Intersect` returns those in both; `Except` subtracts the
  other set from the receiver and can split an interval into several pieces.
- `Contains(Int64Range)` requires every integer in that interval. Contained endpoints alone are
  insufficient when a gap lies between them.
- `IsSubsetOf` compares complete sets. The empty set is a subset of every set, including itself.
- `Equals` and `GetHashCode` compare normalized membership, independent of input partitioning.
  Hash values are for in-process collections and are not stable serialized identifiers.
- All operations return immutable results and leave their inputs unchanged. Null set operands or
  a null construction sequence throw `ArgumentNullException`.

`Cardinality` is `UInt128`. The entire signed 64-bit domain contains exactly `2^64` integers, which
does not fit in `ulong`; no saturation or floating-point rounding is used. `RangeSet.All` represents
that entire domain in one interval. No calendar units, real-valued intervals, serialization format,
parsing syntax, or infinite bounds are implied.

## Ownership, resources and complexity

The constructor enumerates a finite input sequence once, disposes its enumerator, copies its values,
then normalizes the copy. It does not retain the collection. Iterator exceptions propagate without
being replaced. Caller-owned iterators must make progress and must not mutate concurrently during
construction. Inputs are not subject to an arbitrary interval-count cap; allocation may fail normally
if memory is exhausted. There are no background tasks or hidden global caches.

Instances support concurrent readers and set operations; each returned enumerator has independent
state and should not itself be shared without synchronization. All properties and returned intervals
are immutable. Enumerating a result does not expose its storage for modification.

For input interval counts `n` and `m`:

| Operation | Time | Additional storage |
| --- | --- | --- |
| Constructor | `O(n log n)` | `O(n)` |
| Value or interval membership | `O(log n)` | `O(1)` |
| Union, intersection, difference | `O(n + m)` | `O(n + m)` worst case |
| Subset check | `O(n + m)` | `O(1)` |
| Count, cardinality, indexer | `O(1)` | `O(1)` |

These costs depend on interval count, not the represented number of integers. No performance timing
or zero-allocation claim is made for set operations.

## Build and verify

Install .NET SDK 10.0.303 and PowerShell 7. From a clean committed checkout:

```powershell
pwsh -File tools/Verify.ps1
```

The command builds all projects, checks formatting and the API baseline, runs the tests and example,
packs `.nupkg` and `.snupkg`, verifies archive contents, icon/license hashes, XML docs, PDB identity
and Source Link, then executes a fresh package consumer with an empty cache. CI runs on Windows and
Ubuntu. No solution file is required. See [VERIFICATION.md](VERIFICATION.md), [RELEASE.md](RELEASE.md),
and [provenance](docs/PROVENANCE.md).

The custom license is preserved in [LICENSE.txt](LICENSE.txt). Every package embeds the unchanged
`BenScr_Icon_Master.png` as its icon.
