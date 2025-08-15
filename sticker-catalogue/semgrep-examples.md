# Semgrep!

A helpful reference: https://semgrep.dev/docs/writing-rules/pattern-syntax

First, let's try find an entrypoint. We can do this capturing only
the function header, or the function header _and_ body:

```bash
semgrep --lang java --pattern 'public static void main(...)'
semgrep --lang java --pattern 'public static void main(...) { ... }'
```

We're in a quarkus project - there's no explicit `main`. What about API handlers?
```bash
semgrep --lang java --pattern '@Path($A) public class $C { ... }'
```

Can we list Path to filename mappings? Yes, but we need a full YAML rule:
[rule](semgrep-queries/path-class-mapping.yaml)
```bash
semgrep scan --config semgrep-queries/path-class-mapping.yaml --json --quiet | jq -r '.results[].extra.message'
```

# Function-level dataflow

Let's do something jazzier and try and find instances where we are calling methods on 
things that look like repositories, in REST API handlers, with arguments that come straight from user params.

This is getting really fiddly, and the fun of using YAML as a programming language begins to emerge...

[rule](semgrep-queries/basic-param-no-taint.yaml)
```bash
semgrep scan --config semgrep-queries/basic-param-no-taint.yaml --json --quiet | jq '.results[].extra.message' -r
```

