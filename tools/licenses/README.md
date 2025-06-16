# License Generation Tools

This directory contains tools for generating third-party license information for the sticker-award project.

## Files

- `convert_licenses.py` - Python script that converts Maven license XML to CSV format with SPDX normalization
- `README.md` - This file

## Usage

From the project root directory:

```bash
# Generate license file
make LICENSE-3rdparty.csv

# Clean generated files
make clean
```

Note: The Makefile targets are defined in the project root `Makefile`.

## Process

1. **Maven License Plugin**: Runs `mvn license:aggregate-download-licenses` in the sticker-award project
   - Configured to include only direct dependencies (`includeTransitiveDependencies=false`)
   - Generates XML output at `sticker-award/target/generated-sources/license/licenses.xml`

2. **Python Conversion**: Executes `convert_licenses.py` to:
   - Parse the XML license data
   - Normalize license names to SPDX identifiers (e.g., "Apache License 2.0" → "Apache-2.0")
   - Generate CSV output at project root: `LICENSE-3rdparty.csv`

## Output Format

The generated `LICENSE-3rdparty.csv` contains:
- **Component**: Project name ("sticker-award")
- **Origin**: Maven coordinates (maven:groupId:artifactId:jar:version)
- **License**: SPDX license identifier
- **Copyright**: Empty (would need manual research to populate)

## Direct Dependencies

Currently tracks ~16 direct dependencies instead of 330+ transitive dependencies for easier license compliance management.