# Release Notes

All notable changes to this project are documented in this file.

The format is inspired by Keep a Changelog and uses semantic versioning.

## [Unreleased]

### Added

- Added a canonical documentation reference at `DOCUMENTATION.md`.
- Added maintainer/release process docs:
  - `DEVGUIDE.md`
  - `MAINTAINERS.md`
- Added contribution governance updates:
  - stronger contribution requirements in `.github/CONTRIBUTING.md`
  - PR checklist in `.github/PULL_REQUEST_TEMPLATE.md`

### Changed

- Upgraded to modern OpenAPI + Swashbuckle stack and migrated API usage to current OpenAPI model.
- Aligned OData package versions for dependency compatibility.
- Migrated test stack to MSTest + Microsoft.Testing.Platform and enabled analyzers.
- Removed FluentAssertions from tests.
- Updated packaging/docs workflow:
  - package readme is now included in NuGet package
  - pack target focuses on the library project

### Fixed

- Resolved PR #137 review issues and endpoint/query-options wiring mismatches.
- Fixed multiple path handling and route prefix trimming edge cases.
- Eliminated build warnings across solution and CI builds.

## [2.0.0] - TBD

Release date is set when tag `v2.0.0` is created.

## [1.x and earlier]

Historical releases before introducing this file are available through GitHub releases and tags.
