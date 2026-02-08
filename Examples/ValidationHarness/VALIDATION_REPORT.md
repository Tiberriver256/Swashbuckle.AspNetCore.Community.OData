# Validation Harness Report (Template)

> Copy this template into `Examples/ValidationHarness/reports/` for each PR/release validation run.

**Date:** YYYY-MM-DD  
**Branch:** <branch-name>  
**PR:** #<id>  
**Commit:** <sha>

## 🎯 Validation Scope

Describe what was validated (feature area, migration, bugfix, release candidate, etc.).

## ✅ Test Environment

- **Runtime:** .NET <version>
- **OData Version:** <version>
- **Swashbuckle Version:** <version>
- **OpenApi.OData Version:** <version>
- **Test URL:** <url>
- **OS:** <os>

## 📊 Test Results Summary

| Category | Tests | Passed | Failed | Status |
|----------|-------|--------|--------|--------|
| Swagger UI Loading |  |  |  | ☐ PASS / ☐ FAIL |
| OData Query Options |  |  |  | ☐ PASS / ☐ FAIL |
| HTTP Methods |  |  |  | ☐ PASS / ☐ FAIL |
| Method Overloads |  |  |  | ☐ PASS / ☐ FAIL |
| OData Functions |  |  |  | ☐ PASS / ☐ FAIL |
| OData Actions |  |  |  | ☐ PASS / ☐ FAIL |
| Singletons |  |  |  | ☐ PASS / ☐ FAIL |
| Complex Types |  |  |  | ☐ PASS / ☐ FAIL |
| $ref Paths |  |  |  | ☐ PASS / ☐ FAIL |
| Multi-Version API |  |  |  | ☐ PASS / ☐ FAIL |
| **TOTAL** |  |  |  | ☐ PASS / ☐ FAIL |

## 🔍 Key Findings

- Finding 1
- Finding 2
- Finding 3

## 🐛 Issues Found

List defects discovered during validation.

| Severity | Area | Description | Workaround | Issue Link |
|----------|------|-------------|------------|------------|
|          |      |             |            |            |

## 📝 Notes

Include links to screenshots, raw OpenAPI documents, logs, or command output where relevant.

## 🎉 Conclusion

**Status:** ☐ VALIDATION PASSED / ☐ VALIDATION FAILED

### Release Readiness

- [ ] Ready to merge
- [ ] Requires follow-up fixes
- [ ] Requires re-validation
