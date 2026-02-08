# DEVGUIDE

Maintainer guide for building, validating, documenting, and releasing `Swashbuckle.AspNetCore.Community.OData`.

## Core principles

- Keep the package lean and focused on OData + Swashbuckle integration.
- Keep docs, tests, and release notes in sync with code changes.
- Treat the enhanced generator as the default API surface.
- Keep legacy APIs only as compatibility shims during migration windows.
- Do not ship with failing CI or known dependency incompatibilities.

## Local validation checklist

Run before opening or merging PRs:

```bash
dotnet build Swashbuckle.AspNetCore.Community.OData.sln -c Release
dotnet test --project Tests/Swashbuckle.AspNetCore.Community.OData.Test/Swashbuckle.AspNetCore.Community.OData.Test.csproj -c Release
dotnet cake --target=Default
```

Optional sample validation harness:

- `Examples/ValidationHarness/VALIDATION_CHECKLIST.md`
- `Examples/ValidationHarness/VALIDATION_REPORT.md`

## Documentation policy

Any behavior-affecting change should update relevant documentation:

- `README.md` (entry-level usage)
- `DOCUMENTATION.md` (canonical reference)
- `RELEASE_NOTES.md` (change and compatibility history)
- `ENHANCED_FEATURES.md` (feature-oriented deep-dive, when applicable)

## Dependency policy

- Keep OData dependencies aligned (`Microsoft.OData.Core` and `Microsoft.OData.Edm`).
- Review release notes before adopting major version upgrades.
- Prefer staged upgrades when OpenAPI object model changes are involved.

Recommended checks:

```bash
dotnet-outdated Swashbuckle.AspNetCore.Community.OData.sln
dotnet-outdated Examples/ValidationHarness/ValidationHarness.csproj
```

## Release process

1. Ensure `main` is green and up to date.
2. Update `RELEASE_NOTES.md`:
   - move relevant items from `Unreleased` into a new version section.
   - call out breaking changes clearly.
3. Commit release notes and documentation updates.
4. Create and push a semantic version tag using the `v` prefix (for example `v2.0.0`).
   - Package version is derived from git tags via MinVer.
5. Publish GitHub release from the tag.
6. Verify NuGet package version and published release notes match the tag.

### NuGet trusted publishing prerequisites

- Configure a nuget.org Trusted Publisher policy for this repository:
  - Repository owner: `Tiberriver256`
  - Repository: `Swashbuckle.AspNetCore.Community.OData`
  - Workflow file: `build.yml`
  - Environment: `NuGet`
- Add repository/environment secret `NUGET_USER` with the nuget.org profile name used for publishing.

## CI model summary

- Build workflow runs on push, PR, release, and manual dispatch.
- Matrix build across Ubuntu, Windows, and macOS.
- Packaging and publish jobs are release-aware.

## PR expectations for maintainers

Before merge, verify:

- tests are present/updated
- docs are updated
- release notes entry exists when relevant
- CI checks pass on all required jobs
