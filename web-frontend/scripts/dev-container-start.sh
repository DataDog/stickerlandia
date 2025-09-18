#!/bin/bash
# Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
# This product includes software developed at Datadog (https://www.datadoghq.com/).
# Copyright 2025-Present Datadog, Inc.


set -e

# Restore packages - in dev mode, we have to do this at runtime,
# so that we can mount an empty node_modules over the top of our
# source mount from the host. If we _don't_ do this, we'll have issues
# trying to re-use platform specific modules from the source.
npm install

# Start it!
npm run dev -- --host 0.0.0.0 --port 8080
