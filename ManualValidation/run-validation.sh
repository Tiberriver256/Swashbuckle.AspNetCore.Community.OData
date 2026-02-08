#!/bin/bash

# Manual Validation Runner for Enhanced OData Swagger
# This script builds and runs the validation API

set -e

echo "=========================================="
echo "Enhanced OData Swagger - Manual Validation"
echo "=========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}Error: dotnet CLI not found${NC}"
    echo "Please install .NET SDK 8.0 or later"
    exit 1
fi

echo -e "${GREEN}✓ dotnet CLI found${NC}"
echo ""

# Get the directory of this script
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

# Restore packages
echo "Restoring packages..."
dotnet restore --verbosity quiet
echo -e "${GREEN}✓ Packages restored${NC}"
echo ""

# Build the project
echo "Building project..."
dotnet build --verbosity quiet --no-restore
echo -e "${GREEN}✓ Build successful${NC}"
echo ""

# Check for build warnings
BUILD_OUTPUT=$(dotnet build --no-restore 2>&1)
if echo "$BUILD_OUTPUT" | grep -q "warning"; then
    echo -e "${YELLOW}⚠ Build warnings detected:${NC}"
    echo "$BUILD_OUTPUT" | grep "warning" || true
    echo ""
fi

# Run the API
echo "=========================================="
echo "Starting validation API..."
echo "=========================================="
echo ""
echo "Once started, open:"
echo "  • Swagger UI:    http://localhost:5000/swagger"
echo "  • OpenAPI JSON:  http://localhost:5000/swagger/v1/swagger.json"
echo ""
echo -e "${YELLOW}Press Ctrl+C to stop${NC}"
echo ""

# Run with detailed logging
dotnet run --no-build --verbosity normal
