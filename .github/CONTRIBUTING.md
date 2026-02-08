# Contributing

Thank you for contributing to `Swashbuckle.AspNetCore.Community.OData`.

## Before opening a PR

1. Check [open issues](https://github.com/tiberriver256/Swashbuckle.AspNetCore.Community.OData/issues).
2. For non-trivial changes, open/discuss an issue first.
3. Make sure your proposal fits the package scope: OData + Swashbuckle OpenAPI generation.

## Development workflow

### Build and test locally

```bash
dotnet build Swashbuckle.AspNetCore.Community.OData.sln -c Release
dotnet test --project Tests/Swashbuckle.AspNetCore.Community.OData.Test/Swashbuckle.AspNetCore.Community.OData.Test.csproj -c Release
dotnet cake --target=Default
```

### Code quality expectations

- Fix style/analyzer warnings in touched code.
- Add or update tests for behavior changes.
- Keep API behavior and docs aligned.

## Documentation requirements

For behavior-impacting changes, update the relevant documentation:

- `README.md` for top-level usage
- `DOCUMENTATION.md` for detailed behavior/reference
- `ENHANCED_FEATURES.md` for feature-centric examples (when needed)
- `RELEASE_NOTES.md` for notable changes and breaking changes

PRs that change behavior without doc updates may be asked to add docs before merge.

## Pull request checklist

- [ ] Linked related issue(s)
- [ ] Added/updated tests
- [ ] Updated docs where applicable
- [ ] Added/updated release notes where applicable
- [ ] Verified local build/test pass

## Branch and merge notes

- Keep PRs focused and reasonably scoped.
- Include migration notes when introducing breaking changes.
- Do not merge with failing required checks.
