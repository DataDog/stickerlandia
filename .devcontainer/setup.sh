#!/bin/bash
# Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
# This product includes software developed at Datadog (https://www.datadoghq.com/).
# Copyright 2025-Present Datadog, Inc.

echo "Setting up Stickerlandia environment..."

# Set up Git safe directory (for the workspace)
git config --global --add safe.directory /workspace

# Install mise
if ! command -v mise &> /dev/null; then
    echo "Installing mise..."
    curl https://mise.run | sh
    echo 'eval "$(~/.local/bin/mise activate bash)"' >> ~/.bashrc
    export PATH="$HOME/.local/bin:$PATH"
    eval "$(~/.local/bin/mise activate bash)"
fi

# Trust the workspace mise config
mise trust /workspace

# Start services using prebuilt images
echo "Starting stickerlandia..."
mise run compose:deploy:release

echo ""
echo "Development environment setup complete!"
echo ""

# Determine base URL based on environment
if [ -n "$CODESPACE_NAME" ]; then
    BASE_URL="https://${CODESPACE_NAME}"
    echo "Running in GitHub Codespaces!"
    echo ""
    echo "Available services:"
    echo "  - Main Application: ${BASE_URL}-8080.app.github.dev"
    echo "  - Traefik Dashboard: ${BASE_URL}-8081.app.github.dev/dashboard/"
    echo "  - Redpanda Console: ${BASE_URL}-8082.app.github.dev"
    echo "  - MinIO Console: ${BASE_URL}-9001.app.github.dev"
else
    echo "Running locally!"
    echo ""
    echo "Available services:"
    echo "  - Main Application: http://localhost:8080"
    echo "  - Traefik Dashboard: http://localhost:8081/dashboard/"
    echo "  - Redpanda Console: http://localhost:8082"
    echo "  - MinIO Console: http://localhost:9001"
fi

echo ""
echo "To start all services with hot reload: mise run compose:dev"
echo "To stop all services: mise run compose:down"
echo ""

if [ -n "$CODESPACE_NAME" ]; then
    echo "Click on the links above or check the 'Ports' tab for easy access"
else
    echo "Check out the 'Ports' tab at the bottom of the screen to hit these URLs from your browser"
fi

