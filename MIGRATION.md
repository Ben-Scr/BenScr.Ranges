# Compatibility

BenScr.Ranges 1.0.0 is a new standalone library. It does not replace an existing package ID or
promise source compatibility with another interval API. The package, assembly, namespace and
repository all use `BenScr.Ranges`.

`Int64Range` is inclusive at both ends; it is not interchangeable with `System.Range`, array slices
or half-open intervals. Explicitly account for the upper endpoint when adapting another format;
blindly adding or subtracting one can overflow at Int64 limits. Empty values use `RangeSet.Empty`,
not a reversed interval. `Count` means normalized intervals; `Cardinality` means represented values.

The initial target is .NET 10. No earlier .NET or Unity compatibility is claimed. Public API shape is
recorded in `docs/PublicApi.txt`; changes must be reviewed together with behavioral compatibility.
