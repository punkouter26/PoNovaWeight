#!/usr/bin/env pwsh
<#
.SYNOPSIS
Runs tests with code coverage analysis and generates HTML report.

.DESCRIPTION
Executes the full test suite with code coverage enabled, generates an
OpenCover XML report, and provides a summary of coverage by project.

.EXAMPLE
.\scripts\run-coverage.ps1

.NOTES
Requires: dotnet SDK, ReportGenerator (optional for HTML)
#>

param(
    [switch]$OpenReport = $false,
    [string]$MinimumCoverageThreshold = "80"
)

Write-Host "🧪 Running tests with code coverage..." -ForegroundColor Cyan

$ErrorActionPreference = "Stop"

# Clean previous coverage reports
if (Test-Path "coverage") {
    Remove-Item "coverage" -Recurse -Force
}

# Run tests with coverage
dotnet test PoNovaWeight.sln `
    --configuration Release `
    --logger "console;verbosity=minimal" `
    --settings .runsettings `
    /p:CollectCoverage=true `
    /p:CoverageFormat=opencover `
    /p:CoverageDirectory=./coverage `
    /p:Exclude="[*Tests]*,[*]*Tests.*"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Tests failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`n📊 Coverage Summary:" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

# Parse coverage report to show summary
$coverageFiles = Get-ChildItem -Path "coverage" -Filter "coverage.opencover.xml" -Recurse

if ($coverageFiles) {
    Write-Host "✅ Coverage reports generated:" -ForegroundColor Green
    foreach ($file in $coverageFiles) {
        Write-Host "   📄 $($file.FullName)" -ForegroundColor Green
    }
    
    # If ReportGenerator is available, generate HTML
    $reportGeneratorPath = (Get-Command reportgenerator -ErrorAction SilentlyContinue).Source
    if ($reportGeneratorPath) {
        Write-Host "`n📈 Generating HTML report..." -ForegroundColor Cyan
        reportgenerator -reports:"coverage/coverage.opencover.xml" `
                       -targetdir:"coverage/report" `
                       -reporttypes:"HtmlInline" `
                       -assemblyfilters:"+PoNovaWeight*" `
                       -filefilters:"-*.xaml"
        
        $htmlReport = "coverage/report/index.html"
        if (Test-Path $htmlReport) {
            Write-Host "✅ HTML report generated: $htmlReport" -ForegroundColor Green
            if ($OpenReport) {
                Invoke-Item $htmlReport
            }
        }
    } else {
        Write-Host "`n💡 Install ReportGenerator for HTML reports: dotnet tool install -g dotnet-reportgenerator-globaltool" -ForegroundColor Yellow
    }
} else {
    Write-Host "⚠️  No coverage reports found" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n================================================" -ForegroundColor Cyan
Write-Host "✅ Coverage analysis complete!" -ForegroundColor Green
Write-Host "`n📌 Coverage Goals:" -ForegroundColor Cyan
Write-Host "   • Overall: >75% (measure of risk)" -ForegroundColor Gray
Write-Host "   • API Core Logic: >85% (must-have)" -ForegroundColor Gray
Write-Host "   • Client Components: >70% (nice-to-have)" -ForegroundColor Gray
Write-Host "   • Infrastructure: >60% (lower priority)" -ForegroundColor Gray
