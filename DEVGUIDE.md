# DEVGUIDE

Maintainer guide for building, validating, documenting, and releasing `Swashbuckle.AspNetCore.Community.OData`.

## Core principles

- Keep the package lean and focused on OData + Swashbuckle integration.
- Keep docs, tests, and release notes in sync with code changes.
- Do not ship with failing CI or known dependency incompatibilities.

## Local validation checklist

Run before opening or merging PRs:

```bash
dotnet build Swashbuckle.AspNetCore.Community.OData.sln -c Release
dotnet test --project Tests/Swashbuckle.AspNetCore.Community.OData.Test/Swashbuckle.AspNetCore.Community.OData.Test.csproj -c Release
dotnet cake --target=Default
```

Optional manual validation:

- `ManualValidation/VALIDATION_CHECKLIST.md`
- `ManualValidation/VALIDATION_REPORT.md`

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
dotnet-outdated ManualValidation/ManualValidation.csproj
```

## Release process

1. Ensure `main` is green and up to date.
2. Bump package version metadata in the library project file.
3. Update `RELEASE_NOTES.md`:
   - move relevant items from `Unreleased` into a new version section.
   - call out breaking changes clearly.
4. Verify package metadata, docs links, and version consistency.
5. Create and push version tag from that commit (for example `v2.0.0`).
6. Publish GitHub release from the tag.
7. Verify NuGet package version and published release notes match the tag.

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
