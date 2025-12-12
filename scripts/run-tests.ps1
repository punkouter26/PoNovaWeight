<#
.SYNOPSIS
    Runs tests for the Nova Food Journal solution.

.DESCRIPTION
    This script runs unit tests, integration tests, or all tests with various options.
    Integration tests require Azurite to be running.

.PARAMETER Type
    Type of tests to run: All, Unit, Integration (default: Unit)

.PARAMETER Verbose
    Show detailed test output

.PARAMETER Filter
    Filter tests by name pattern

.EXAMPLE
    ./run-tests.ps1
    
.EXAMPLE
    ./run-tests.ps1 -Type All -Verbose

.EXAMPLE
    ./run-tests.ps1 -Filter "DailyLog"
#>

param(
    [ValidateSet("All", "Unit", "Integration")]
    [string]$Type = "Unit",
    [switch]$Verbose,
    [string]$Filter
)

$ErrorActionPreference = "Stop"
$solutionRoot = Split-Path -Parent $PSScriptRoot

Write-Host "🧪 Nova Food Journal - Test Runner" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host "   Test Type: $Type" -ForegroundColor White
Write-Host "   Solution: $solutionRoot" -ForegroundColor White

# Build the solution first
Write-Host "`n📦 Building solution..." -ForegroundColor Yellow
Push-Location $solutionRoot
try {
    dotnet build --nologo -v q
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "   ✅ Build succeeded" -ForegroundColor Green
} finally {
    Pop-Location
}

# Construct test filter
$testFilter = switch ($Type) {
    "Unit" { "FullyQualifiedName!~Integration" }
    "Integration" { "FullyQualifiedName~Integration" }
    "All" { $null }
}

if ($Filter) {
    if ($testFilter) {
        $testFilter = "($testFilter) & (FullyQualifiedName~$Filter)"
    } else {
        $testFilter = "FullyQualifiedName~$Filter"
    }
}

# Check for Azurite if running integration tests
if ($Type -eq "Integration" -or $Type -eq "All") {
    Write-Host "`n🔍 Checking Azurite status..." -ForegroundColor Yellow
    try {
        $tcpConnection = Test-NetConnection -ComputerName "127.0.0.1" -Port 10002 -WarningAction SilentlyContinue
        if (-not $tcpConnection.TcpTestSucceeded) {
            Write-Host "   ⚠️ Azurite is not running. Integration tests may be skipped." -ForegroundColor Yellow
            Write-Host "   Run './scripts/setup-azurite.ps1' to start Azurite" -ForegroundColor Yellow
        } else {
            Write-Host "   ✅ Azurite is running" -ForegroundColor Green
        }
    } catch {
        Write-Host "   ⚠️ Could not check Azurite status" -ForegroundColor Yellow
    }
}

# Run tests
Write-Host "`n🚀 Running tests..." -ForegroundColor Yellow

$testArgs = @(
    "test",
    "--nologo"
)

if ($testFilter) {
    $testArgs += "--filter"
    $testArgs += $testFilter
}

if ($Verbose) {
    $testArgs += "-v"
    $testArgs += "normal"
} else {
    $testArgs += "-v"
    $testArgs += "minimal"
}

Push-Location $solutionRoot
try {
    $startTime = Get-Date
    
    & dotnet @testArgs
    $testExitCode = $LASTEXITCODE
    
    $duration = (Get-Date) - $startTime
    
    Write-Host "`n" -NoNewline
    if ($testExitCode -eq 0) {
        Write-Host "✅ All tests passed!" -ForegroundColor Green
    } else {
        Write-Host "❌ Some tests failed" -ForegroundColor Red
    }
    
    Write-Host "   Duration: $($duration.TotalSeconds.ToString('F2')) seconds" -ForegroundColor White
    
    exit $testExitCode
} finally {
    Pop-Location
}
