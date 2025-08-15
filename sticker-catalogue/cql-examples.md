# CodeQL

First, let's create a CodeQL database from our Java codebase:

```bash
codeql database create cql-db \
  --language=java --source-root=. \
  --command 'mvn -B -Pno-static-analysis clean compile'
```

Now we can start running queries against it. We've got a few categories of queries to play with.

## Basics - Query Packs

We can use the built-in java security pack to see if we have anything troubling:
```bash
codeql database analyze cql-db --format=csv --output=results.csv codeql/java-queries:codeql-suites/java-security-and-quality.qls
cat results.csv
```

## Debug and exploration

Want to see what classes we have?
[query](cql-queries/debug/all-classes.ql)
```bash
codeql query run cql-queries/debug/all-classes.ql -d cql-db
```

Or, explore all the import statements in the codebase:
[query](cql-queries/debug/all-imports.ql)
```bash
codeql query run cql-queries/debug/all-imports.ql -d cql-db
```

## Architecture enforcement

We can check if our REST controllers are properly layered - they shouldn't be using entity classes:
[query](cql-queries/arch-rules/rest-api-no-entities.ql)

```bash
codeql query run cql-queries/arch-rules/rest-api-no-entities.ql -d cql-db
```






# Too Much Detail!
## Taint tracking
This is where things get interesting. We can track data flows from HTTP parameters all the way to Panache operations:

[query](cql-queries/taint-tracking/global-flow.ql)
```bash
codeql query run cql-queries/taint-tracking/global-flow.ql -d cql-db
```

Or look for potentially vulnerable flows from HTTP to ORM methods:

```bash
codeql query run cql-queries/taint-tracking/http-to-orm-data-flow.ql -d cql-db
```