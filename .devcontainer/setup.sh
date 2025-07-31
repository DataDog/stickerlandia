#!/bin/bash

echo "🚀 Setting up Stickerlandia development environment..."

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
make compose-dev-up

echo "✅ Development environment setup complete!"
echo ""
echo "📝 Available services:"
echo "  - Main Application: http://localhost:8080"
echo "  - Traefik Dashboard: http://localhost:8081/dashboard/"
echo "  - Redpanda Console: http://localhost:8082"
echo "  - MinIO Console: http://localhost:9001"
echo ""
echo "🏃 To start all services with hot reload: make compose-dev-up"
echo "🛑 To stop all services: make compose-down"
echo ""
echo " Check out the 'Ports' tab at the bottom of the screen to hit these URLs from your browser"

