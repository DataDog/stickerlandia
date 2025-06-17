# Semgrep Queries

Semgrep rules for static analysis and architecture validation of the sticker-award application.

## Architecture Rules

| Rule | Purpose |
|------|---------|
| [rest-api-no-entity-imports.yaml](arch-rules/rest-api-no-entity-imports.yaml) | Prevents REST controllers from importing entity classes directly |
| [cross-domain-violations.yaml](arch-rules/cross-domain-violations.yaml) | Prevents StickerRepository from importing award domain classes |

## Usage

```bash
# Run all architecture rules
semgrep --config=semgrep-queries/arch-rules/ src/

# Run a specific rule
semgrep --config=semgrep-queries/arch-rules/rest-api-no-entity-imports.yaml src/

# Run with verbose output
semgrep --config=semgrep-queries/arch-rules/ src/ --verbose
```

## Comparison with CodeQL

- **Simpler syntax**: YAML-based rules vs. CodeQL's query language
- **Faster execution**: Pattern matching vs. full semantic analysis
- **Broader language support**: Works across many languages
- **Less precision**: Pattern-based vs. full program analysis
- **Easier to write**: Straightforward patterns vs. complex queries

Semgrep is ideal for catching common anti-patterns and enforcing coding standards, while CodeQL excels at complex data flow analysis and deep semantic understanding.