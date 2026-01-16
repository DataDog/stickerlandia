#!/bin/bash
# Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
# This product includes software developed at Datadog (https://www.datadoghq.com/).
# Copyright 2025-Present Datadog, Inc.

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