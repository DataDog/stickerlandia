#!/bin/bash

# Architecture Rule Validation Script
# This script validates that no architecture rules are violated in the codebase

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
CQL_DB_DIR="$PROJECT_DIR/cql-db"
ARCH_RULES_DIR="$PROJECT_DIR/cql-queries/arch-rules"
RESULTS_DIR="$PROJECT_DIR/build/codeql-results"

echo "🔍 Architecture Rule Validation"
echo "Project: $PROJECT_DIR"
echo ""

# Create results directory
mkdir -p "$RESULTS_DIR"

# Check if CodeQL is installed
if ! command -v codeql &> /dev/null; then
    echo "❌ CodeQL CLI not found. Please install CodeQL CLI and add it to your PATH."
    echo "   Download from: https://github.com/github/codeql-cli-binaries/releases"
    exit 1
fi

echo "✅ CodeQL CLI found: $(codeql version --format=text)"

# Check if database exists
if [ ! -d "$CQL_DB_DIR" ]; then
    echo "❌ CodeQL database not found at $CQL_DB_DIR"
    echo "   Please create the database first by running:"
    echo "   codeql database create cql-db --language=java --source-root=."
    exit 1
fi

echo "✅ CodeQL database found: $CQL_DB_DIR"

# Check if architecture rules exist
if [ ! -d "$ARCH_RULES_DIR" ]; then
    echo "❌ Architecture rules directory not found at $ARCH_RULES_DIR"
    exit 1
fi

# Count available rules
RULE_COUNT=$(find "$ARCH_RULES_DIR" -name "*.ql" | wc -l)
echo "✅ Found $RULE_COUNT architecture rule(s)"

# Run all architecture rules
echo ""
echo "🚀 Running architecture rules..."

VIOLATION_COUNT=0
RULES_RUN=0

for rule_file in "$ARCH_RULES_DIR"/*.ql; do
    if [ -f "$rule_file" ]; then
        rule_name=$(basename "$rule_file" .ql)
        echo "  📋 Checking: $rule_name"
        
        # Run the rule
        result_file="$RESULTS_DIR/${rule_name}.sarif"
        
        if codeql database analyze "$CQL_DB_DIR" "$rule_file" \
            --format=sarif-latest \
            --output="$result_file" \
            --quiet; then
            
            # Check if there are any violations
            violations=$(jq '.runs[0].results | length' "$result_file" 2>/dev/null || echo "0")
            
            if [ "$violations" -gt 0 ]; then
                echo "     ❌ $violations violation(s) found"
                
                # Show details of violations
                jq -r '.runs[0].results[] | "       • \(.message.text) (\(.locations[0].physicalLocation.artifactLocation.uri):\(.locations[0].physicalLocation.region.startLine))"' "$result_file" 2>/dev/null || true
                
                VIOLATION_COUNT=$((VIOLATION_COUNT + violations))
            else
                echo "     ✅ No violations"
            fi
            
            RULES_RUN=$((RULES_RUN + 1))
        else
            echo "     ❌ Failed to run rule"
            exit 1
        fi
    fi
done

echo ""
echo "📊 Summary:"
echo "   Rules executed: $RULES_RUN"
echo "   Total violations: $VIOLATION_COUNT"

if [ "$VIOLATION_COUNT" -gt 0 ]; then
    echo ""
    echo "❌ Architecture validation FAILED with $VIOLATION_COUNT violation(s)"
    echo "   Please fix the violations before proceeding."
    echo "   Results saved to: $RESULTS_DIR"
    exit 1
else
    echo ""
    echo "✅ Architecture validation PASSED - no violations found"
    exit 0
fi