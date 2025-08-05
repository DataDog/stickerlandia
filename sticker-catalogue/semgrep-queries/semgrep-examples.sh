#!/bin/bash

# A helpful reference:
# https://semgrep.dev/docs/writing-rules/pattern-syntax

# Find the entrypoint
semgrep --lang java --pattern 'public static void main(...)'

# Find the entrypoint with the body
semgrep --lang java --pattern 'public static void main(...) { ... }'


#
# We're in a quarkus project - there's no explicit main.
# What about API handlers?
#
semgrep --lang java --pattern '@Path($A) public class $C { ... }'

# Can we list PATH to filename mappings?
# Not without using a full yaml rule ... 
semgrep scan --config semgrep-queries/path-class-mapping.yaml --json | jq -r '.results[].extra.message'

#
# Let's do something jazzier and try and find
# instances where we are calling methods on things
# that look like repositories, in REST API handlers,
# with arguments that come straight from user params.
# 
# This is getting really fiddly, and the fun of using yaml
# as a programming language begins to emerge ...
semgrep scan --config semgrep-queries/basic-param-no-taint.yaml --json | jq '.results[].extra.message' -r

#
# What about instances with indirect dataflow?
# Check out: data flows into StickerRepository::updateStickerImageKey
#
semgrep scan --config semgrep-queries/basic-param-taint.yaml --json | jq . '.results[]'
