<#
.SYNOPSIS
    Generates code coverage reports for the Nova Food Journal solution.

.DESCRIPTION
    This script runs tests with coverage collection and generates HTML reports
    using ReportGenerator. Coverage data is collected using Coverlet.

.PARAMETER Threshold
    Minimum coverage percentage required (default: 80)

.PARAMETER Open
    Open the HTML report in the default browser after generation

.PARAMETER OutputPath
    Output directory for coverage reports (default: docs/coverage)

.EXAMPLE
    ./generate-coverage.ps1
    
.EXAMPLE
    ./generate-coverage.ps1 -Threshold 90 -Open

.EXAMPLE
    ./generate-coverage.ps1 -OutputPath "./coverage-reports"
#>

param(
    [int]$Threshold = 80,
    [switch]$Open,
    [string]$OutputPath = "docs/coverage"
)

$ErrorActionPreference = "Stop"
$solutionRoot = Split-Path -Parent $PSScriptRoot

Write-Host "📊 Nova Food Journal - Coverage Report Generator" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "   Threshold: $Threshold%" -ForegroundColor White
Write-Host "   Output: $OutputPath" -ForegroundColor White

# Ensure ReportGenerator is installed
Write-Host "`n📦 Checking ReportGenerator..." -ForegroundColor Yellow
$reportGeneratorPath = "$env:USERPROFILE\.dotnet\tools\reportgenerator.exe"
if (-not (Test-Path $reportGeneratorPath)) {
    Write-Host "   Installing ReportGenerator..." -ForegroundColor Yellow
    dotnet tool install -g dotnet-reportgenerator-globaltool
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   ❌ Failed to install ReportGenerator" -ForegroundColor Red
        exit 1
    }
}
Write-Host "   ✅ ReportGenerator is available" -ForegroundColor Green

# Clean previous coverage results
$coverageDir = Join-Path $solutionRoot "TestResults"
$reportDir = Join-Path $solutionRoot $OutputPath

Write-Host "`n🧹 Cleaning previous results..." -ForegroundColor Yellow
if (Test-Path $coverageDir) {
    Remove-Item -Path $coverageDir -Recurse -Force
}
if (Test-Path $reportDir) {
    Remove-Item -Path $reportDir -Recurse -Force
}
New-Item -ItemType Directory -Path $reportDir -Force | Out-Null

# Run tests with coverage
Write-Host "`n🧪 Running tests with coverage..." -ForegroundColor Yellow
Push-Location $solutionRoot
try {
    dotnet test `
        --nologo `
        --collect:"XPlat Code Coverage" `
        --results-directory $coverageDir `
        --filter "FullyQualifiedName!~Integration" `
        -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura `
        -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Exclude="[*]*.Program,[*]*.Migrations.*"

    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Tests failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "   ✅ Tests completed" -ForegroundColor Green
} finally {
    Pop-Location
}

# Find coverage files
Write-Host "`n📁 Locating coverage files..." -ForegroundColor Yellow
$coverageFiles = Get-ChildItem -Path $coverageDir -Recurse -Filter "coverage.cobertura.xml"
if ($coverageFiles.Count -eq 0) {
    Write-Host "   ❌ No coverage files found" -ForegroundColor Red
    exit 1
}
Write-Host "   Found $($coverageFiles.Count) coverage file(s)" -ForegroundColor Green

$coverageFilePaths = $coverageFiles | ForEach-Object { $_.FullName }
$coverageReports = $coverageFilePaths -join ";"

# Generate HTML report
Write-Host "`n📊 Generating HTML report..." -ForegroundColor Yellow
& reportgenerator `
    "-reports:$coverageReports" `
    "-targetdir:$reportDir" `
    "-reporttypes:Html;Badges;TextSummary" `
    "-title:Nova Food Journal Coverage" `
    "-assemblyfilters:+PoNovaWeight.*;-*.Tests"

if ($LASTEXITCODE -ne 0) {
    Write-Host "   ❌ Report generation failed" -ForegroundColor Red
    exit 1
}
Write-Host "   ✅ Report generated" -ForegroundColor Green

# Parse coverage summary
Write-Host "`n📈 Coverage Summary:" -ForegroundColor Yellow
$summaryFile = Join-Path $reportDir "Summary.txt"
if (Test-Path $summaryFile) {
    $summary = Get-Content $summaryFile
    $summary | ForEach-Object { Write-Host "   $_" -ForegroundColor White }
    
    # Extract line coverage percentage
    $lineCoverage = $summary | Where-Object { $_ -match "Line coverage:" }
    if ($lineCoverage -match "(\d+\.?\d*)%") {
        $coverage = [double]$Matches[1]
        Write-Host "`n" -NoNewline
        if ($coverage -ge $Threshold) {
            Write-Host "✅ Coverage ($coverage%) meets threshold ($Threshold%)" -ForegroundColor Green
        } else {
            Write-Host "❌ Coverage ($coverage%) below threshold ($Threshold%)" -ForegroundColor Red
            exit 1
        }
    }
}

# Open report if requested
$reportPath = Join-Path $reportDir "index.html"
if ($Open -and (Test-Path $reportPath)) {
    Write-Host "`n🌐 Opening report in browser..." -ForegroundColor Yellow
    Start-Process $reportPath
}

Write-Host "`n📋 Report location: $reportPath" -ForegroundColor Cyan
Write-Host ""
