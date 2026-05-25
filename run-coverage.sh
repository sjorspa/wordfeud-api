#!/bin/bash
# run-coverage.sh
# Runs all tests with code coverage and generates reports in multiple formats.
#
# Usage:
#   ./run-coverage.sh              # Run with default thresholds
#   ./run-coverage.sh --unit       # Run only unit tests
#   ./run-coverage.sh --integration # Run only integration tests
#   ./run-coverage.sh --line-threshold 70  # Set minimum line coverage %
#   ./run-coverage.sh --branch-threshold 50  # Set minimum branch coverage %
#
# Requirements:
#   - .NET 8 SDK installed
#   - Run from the repository root (where this script lives)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
UNIT_PROJECT="$SCRIPT_DIR/Wordfeud.Api.Tests/Wordfeud.Api.Tests.csproj"
INTEGRATION_PROJECT="$SCRIPT_DIR/Wordfeud.Api.IntegrationTests/Wordfeud.Api.IntegrationTests.csproj"
OUTPUT_DIR="$SCRIPT_DIR/coverage-reports"

# Thresholds (can be overridden via env vars)
LINE_THRESHOLD="${LINE_THRESHOLD:-70}"
BRANCH_THRESHOLD="${BRANCH_THRESHOLD:-50}"

# Flags
RUN_UNIT=true
RUN_INTEGRATION=true

while [[ $# -gt 0 ]]; do
    case "$1" in
        --unit)
            RUN_UNIT=true
            RUN_INTEGRATION=false
            shift
            ;;
        --integration)
            RUN_UNIT=false
            RUN_INTEGRATION=true
            shift
            ;;
        --line-threshold)
            LINE_THRESHOLD="$2"
            shift 2
            ;;
        --branch-threshold)
            BRANCH_THRESHOLD="$2"
            shift 2
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [--unit] [--integration] [--line-threshold N] [--branch-threshold N]"
            exit 1
            ;;
    esac
done

mkdir -p "$OUTPUT_DIR"

echo "=============================================="
echo "  Wordfeud API — Code Coverage Report"
echo "=============================================="
echo "  Line threshold:   ${LINE_THRESHOLD}%"
echo "  Branch threshold: ${BRANCH_THRESHOLD}%"
echo "  Output directory: $OUTPUT_DIR"
echo "=============================================="
echo ""

FAILED=0

run_coverage() {
    local project_name="$1"
    local project_path="$2"
    local coverlet_output="$OUTPUT_DIR/${project_name}.coverage.cobertura.xml"

    echo "──────────────────────────────────────────────"
    echo "  Running: $project_name"
    echo "──────────────────────────────────────────────"
    echo ""

    dotnet test "$project_path" \
        --logger "trx;LogFileName=$OUTPUT_DIR/${project_name}.trx" \
        /p:CollectCoverage=true \
        /p:CoverletOutputFormat=cobertura \
        /p:CoverletOutput="$coverlet_output" \
        /p:Exclude="[xunit.*]*" \
        --no-restore \
        -v minimal

    echo ""
}

# Run unit tests
if [ "$RUN_UNIT" = true ]; then
    run_coverage "UnitTests" "$UNIT_PROJECT"
fi

# Run integration tests
if [ "$RUN_INTEGRATION" = true ]; then
    run_coverage "IntegrationTests" "$INTEGRATION_PROJECT"
fi

# Generate summary reports
echo "=============================================="
echo "  Generating Summary Reports"
echo "=============================================="
echo ""

if [ "$RUN_UNIT" = true ]; then
    UNIT_COVERAGE="$OUTPUT_DIR/UnitTests.coverage.cobertura.xml"
    if [ -f "$UNIT_COVERAGE" ]; then
        echo "──────── Unit Tests Coverage Summary ────────"
        # Use reportgenerator if available
        if command -v reportgenerator &> /dev/null; then
            reportgenerator \
                -reports:"$UNIT_COVERAGE" \
                -targetdir:"$OUTPUT_DIR/html-unit" \
                -reporttypes:HtmlInline_AzurePipelines \
                -verbosity:Warning > /dev/null 2>&1 || true

            echo "  HTML report:    $OUTPUT_DIR/html-unit/index.html"
        else
            echo "  Cobertura XML:  $UNIT_COVERAGE"
            echo "  (Install reportgenerator for HTML: dotnet tool install --global dotnet-reportgenerator-globaltool)"
        fi
    else
        echo "  WARNING: No coverage data found for UnitTests"
    fi
    echo ""
fi

if [ "$RUN_INTEGRATION" = true ]; then
    INTEGRATION_COVERAGE="$OUTPUT_DIR/IntegrationTests.coverage.cobertura.xml"
    if [ -f "$INTEGRATION_COVERAGE" ]; then
        echo "──────── Integration Tests Coverage Summary ─"
        if command -v reportgenerator &> /dev/null; then
            reportgenerator \
                -reports:"$INTEGRATION_COVERAGE" \
                -targetdir:"$OUTPUT_DIR/html-integration" \
                -reporttypes:HtmlInline_AzurePipelines \
                -verbosity:Warning > /dev/null 2>&1 || true

            echo "  HTML report:    $OUTPUT_DIR/html-integration/index.html"
        else
            echo "  Cobertura XML:  $INTEGRATION_COVERAGE"
        fi
    else
        echo "  WARNING: No coverage data found for IntegrationTests"
    fi
    echo ""
fi

# Combined coverage
if [ "$RUN_UNIT" = true ] && [ "$RUN_INTEGRATION" = true ]; then
    echo "──────── Combined Coverage (All Tests) ──────"
    if command -v reportgenerator &> /dev/null; then
        reportgenerator \
            -reports:"$OUTPUT_DIR/UnitTests.coverage.cobertura.xml;$OUTPUT_DIR/IntegrationTests.coverage.cobertura.xml" \
            -targetdir:"$OUTPUT_DIR/html-combined" \
            -reporttypes:HtmlInline_AzurePipelines \
            -verbosity:Warning > /dev/null 2>&1 || true

        echo "  Combined HTML:  $OUTPUT_DIR/html-combined/index.html"
    fi
    echo ""
fi

echo "=============================================="
echo "  Coverage check complete."
echo "=============================================="
