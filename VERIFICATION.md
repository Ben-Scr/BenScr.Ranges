# Verification

The release pipeline runs `tools/Verify.ps1` from a clean commit. It restores pinned dependencies
in locked mode, builds four projects with warnings as errors and checked integer arithmetic, verifies
whitespace/style, runs tests and the complete example, compares the public API baseline, packs and
audits the exact archives, and restores/runs an isolated NuGet consumer with an empty package cache.

Behavioral tests include:

- Closed endpoints, singleton/default/empty/full-domain semantics and argument failures.
- Sorting, overlap/adjacency merging, immutable input copying, iterator disposal and failures.
- Union, intersection and subtraction including cuts spanning multiple intervals and Int64 extrema.
- Membership, subset, canonical equality/hash behavior and concurrent independent reads.
- All 65,536 pairs of subsets of an eight-value domain, checked against independent bit membership.
- 2,000 fixed-seed unordered range pairs checked against HashSet membership and set operations.
- Extreme-range algebra and exact counts checked using BigInteger, plus a 10,000-interval case.

CI executes the same workflow on Windows and Ubuntu with SDK 10.0.303 and xUnit.v3 4.0.2 through
Microsoft.Testing.Platform. Platform support is .NET 10; no unexecuted targets are advertised.
The scripts generate per-run TRX, archive audit, consumer evidence and `release-manifest.json` under
ignored `artifacts/verification-<id>/`. Manifest counters distinguish passed, failed and total tests.
CI URLs and public-feed checks are recorded with the release rather than predeclared as successful.

The package audit requires exact expected archive entries, no runtime dependencies, matching
`.nupkg`/`.snupkg` metadata, the original license/icon hashes, Portable PDB identity and checksum-correct
source mappings. Source Link points to the committed public source. Package creation alone does not
prove publication; the separate public-feed consumer is part of release completion.
