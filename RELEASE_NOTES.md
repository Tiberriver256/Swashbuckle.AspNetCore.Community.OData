# Release Notes

All notable changes to this project are documented in this file.

The format is inspired by Keep a Changelog and uses semantic versioning.

## [Unreleased]

### Added

- Added coverage collection (`Microsoft.Testing.Extensions.CodeCoverage`) with Codecov upload in CI.
- Added `Examples/ValidationHarness` to the solution and CI build validation.
- Library now multi-targets `net8.0;net9.0;net10.0`.

### Changed

- Cleared Dependabot backlog: GitHub Actions (`setup-dotnet` 5.2.0, `upload-artifact` 7.0.1, `download-artifact` 8.0.1, `codeql-action` 4.35.4, `release-drafter` 7.3.0) and NuGet (`Swashbuckle.AspNetCore` 10.1.7, `Microsoft.OpenApi.OData` 3.2.1, `MinVer` 7.0.0, `MSTest.Analyzers` 4.2.2, `Microsoft.SourceLink.GitHub` 10.0.300).
- Removed `TreatWarningsAsErrors=False` overrides so the root `TreatWarningsAsErrors=true` quality gate applies to Source, Tests, and samples.
- Fixed machine-specific absolute paths in `Examples/SimpleOdataApi` project file.
- Documented public strong-name key (`Key.snk`) rationale for OSS signing.

### Fixed

- Pinned `Microsoft.OpenApi` to `3.5.5` because `3.5.1` has high severity flaw GHSA-v5pm-xwqc-g5wc.

### Fixed

## [2.0.1] - 2026-02-08

### Added

### Changed

- Standardized MinVer configuration so package versions are derived consistently from git tags.
- Clarified maintainer release workflow for tag-driven versioning in `DEVGUIDE.md`.
- Switched NuGet publishing workflow from long-lived API key auth to nuget.org Trusted Publisher (OIDC via `NuGet/login@v1`).

### Fixed

- Added release workflow validation that checks `.nupkg` version matches the release tag before pushing to NuGet.

## [2.0.0] - 2026-02-08

### Added

- Added a canonical documentation reference at `DOCUMENTATION.md`.
- Added maintainer/release process docs: `DEVGUIDE.md` and `MAINTAINERS.md`.
- Added contribution governance updates in `.github/CONTRIBUTING.md` and `.github/PULL_REQUEST_TEMPLATE.md`.
- Added Swagger UI demo animation (`.webp`) to `README.md`. (#138)

### Changed

- Upgraded to modern OpenAPI + Swashbuckle stack and migrated API usage to the current OpenAPI model.
- Made enhanced OData Swagger registration the default path and marked legacy `AddSwaggerGenOData` as an obsolete compatibility shim.
- Moved the manual validation app into samples as `Examples/ValidationHarness`.
- Aligned OData package versions for dependency compatibility.
- Migrated the test stack to MSTest + Microsoft.Testing.Platform and enabled analyzers.
- Removed FluentAssertions from tests.
- Updated packaging/docs workflow: package README is included in NuGet package, and pack target focuses on the library project.
- Upgraded GitHub Actions workflow action versions to current releases. (#139)

### Fixed

- Resolved PR #137 review issues and endpoint/query-options wiring mismatches.
- Fixed multiple path handling and route prefix trimming edge cases.
- Eliminated build warnings across solution and CI builds.

## [1.x and earlier]

Historical releases before introducing this file are available through GitHub releases and tags.
