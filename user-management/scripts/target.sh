#!/bin/bash
set -e

# Map TARGETPLATFORM to Rust target
case "$TARGETPLATFORM" in
    "linux/amd64")
        export TRACER_ARCHITECTURE="amd64"
        ;;
    "linux/arm64")
        export TRACER_ARCHITECTURE="arm64"
        ;;
    *)
        echo "Unsupported platform: $TARGETPLATFORM"
        exit 1
        ;;
esac