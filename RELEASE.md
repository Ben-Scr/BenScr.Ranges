# Release procedure

1. Review the API baseline, compatibility notes, tests and documentation. Commit a clean source tree.
2. Run `pwsh -File tools/Verify.ps1` and require every check to pass. Push the exact source commit and
   require both Windows and Ubuntu CI verification jobs to succeed.
3. Preserve the exact verified CI `.nupkg`, `.snupkg`, their hashes and manifest. Audit metadata,
   archive contents, original icon/license, PDB identities and live source checksums.
4. Resolve `packages/BenScr.Ranges/1.0.0` to the source commit recorded in the archives. Publish only
   the verified files, without rebuilding them after approval. Never replace an existing version.
5. Publish the NuGet package and symbols, then wait for indexing. Restore and execute a fresh public
   consumer, compare signed package payloads and verify downloaded symbols. Check the gallery icon.
6. Create the GitHub release with the exact verified archives and final verification evidence.

Public-feed consumer, from this repository after indexing:

```powershell
pwsh -File tools/TestPackage.ps1 -Source https://api.nuget.org/v3/index.json `
    -ExpectedCommit <source-commit> -EvidencePath artifacts/public-consumer.json
```

`tools/Verify.ps1 -ReleaseTag packages/BenScr.Ranges/1.0.0` can additionally verify the tag against the
current commit before a new local verification run. Artifacts from different runs remain distinct;
only the specifically audited release archives are published. An upload awaiting processing is not
yet an installable release.
