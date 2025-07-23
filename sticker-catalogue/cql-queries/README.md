# CodeQL Queries

CodeQL queries for analyzing the sticker-award application codebase.

## CI/CD Architecture Rules (Enforced)

| Query | Purpose |
|-------|---------|
| [cross-domain-issues.ql](arch-rules/cross-domain-issues.ql) | Detects cross-domain violations in layered architecture |
| [rest-api-imports-simple.ql](arch-rules/rest-api-imports-simple.ql) | Validates REST API import dependencies |
| [rest-api-no-entities.ql](arch-rules/rest-api-no-entities.ql) | Ensures REST controllers don't directly import entity classes |

## Taint Tracking Analysis (Educational)

| Query | Purpose |
|-------|---------|
| [global-flow.ql](taint-tracking/global-flow.ql) | Modern global data flow tracking from HTTP parameters to Panache operations |
| [http-to-orm-data-flow.ql](taint-tracking/http-to-orm-data-flow.ql) | Taint tracking from HTTP parameters to potentially vulnerable Panache methods |

## Debug Queries

| Query | Purpose |
|-------|---------|
| [all-classes.ql](debug/all-classes.ql) | Lists all classes in the codebase |
| [all-imports.ql](debug/all-imports.ql) | Shows all import statements |
| [class-method-docs.ql](debug/class-method-docs.ql) | Extracts class and method documentation |
| [debug-domains.ql](debug/debug-domains.ql) | Explores domain boundaries |
| [debug-files.ql](debug/debug-files.ql) | Lists source files |
| [debug-import-details.ql](debug/debug-import-details.ql) | Detailed import analysis |
| [debug-import-methods.ql](debug/debug-import-methods.ql) | Method-level import tracking |
| [debug-imports-in-repo.ql](debug/debug-imports-in-repo.ql) | Repository-specific imports |
| [debug-imports.ql](debug/debug-imports.ql) | General import debugging |
| [debug-refs.ql](debug/debug-refs.ql) | Reference analysis |
| [debug-sticker-repo-imports.ql](debug/debug-sticker-repo-imports.ql) | Sticker repository import analysis |

## Usage

```bash
# Run a specific query
codeql query run cql-queries/arch-rules/http-to-orm-data-flow.ql -d cql-db

# Run all architecture rules
codeql query run cql-queries/arch-rules/ -d cql-db
```