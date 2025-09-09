#!/bin/bash

echo "🚀 Setting up Stickerlandia environment..."

# Update package lists
# sudo apt-get update

# Install additional tools that might be needed
# sudo apt-get install -y curl wget jq make

# Install Maven (for Java/Quarkus development)
# sudo apt-get install -y maven

# Set up Git safe directory (for the workspace)
git config --global --add safe.directory /workspace

# Build containers
echo "Starting stickerlandia"
make compose-up

echo "✅ Development environment setup complete!"
echo ""

# Determine base URL based on environment
if [ -n "$CODESPACE_NAME" ]; then
    BASE_URL="https://${CODESPACE_NAME}"
    echo "🌐 Running in GitHub Codespaces!"
    echo ""
    echo "📝 Available services:"
    echo "  - Main Application: ${BASE_URL}-8080.app.github.dev"
    echo "  - Traefik Dashboard: ${BASE_URL}-8081.app.github.dev/dashboard/"
    echo "  - Redpanda Console: ${BASE_URL}-8082.app.github.dev"
    echo "  - MinIO Console: ${BASE_URL}-9001.app.github.dev"
else
    echo "💻 Running locally!"
    echo ""
    echo "📝 Available services:"
    echo "  - Main Application: http://localhost:8080"
    echo "  - Traefik Dashboard: http://localhost:8081/dashboard/"
    echo "  - Redpanda Console: http://localhost:8082"
    echo "  - MinIO Console: http://localhost:9001"
fi

echo ""
echo "🏃 To start all services with hot reload: make compose-dev-up"
echo "🛑 To stop all services: make compose-down"
echo ""

if [ -n "$CODESPACE_NAME" ]; then
    echo "🔗 Click on the links above or check the 'Ports' tab for easy access"
else
    echo "🔗 Check out the 'Ports' tab at the bottom of the screen to hit these URLs from your browser"
fi

