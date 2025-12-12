<#
.SYNOPSIS
    Sets up Azurite for local development storage emulation.

.DESCRIPTION
    This script installs (if needed) and starts Azurite, the Azure Storage emulator.
    Azurite is required for running integration tests and local development.

.PARAMETER Port
    The port for the Table Storage service (default: 10002)

.PARAMETER Location
    The directory for Azurite data files (default: ./azurite-data)

.PARAMETER Silent
    Run Azurite in silent mode without console output

.EXAMPLE
    ./setup-azurite.ps1
    
.EXAMPLE
    ./setup-azurite.ps1 -Location "C:\azurite-data" -Silent
#>

param(
    [int]$Port = 10002,
    [string]$Location = "./azurite-data",
    [switch]$Silent
)

$ErrorActionPreference = "Stop"

Write-Host "🔧 Nova Food Journal - Azurite Setup" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

# Check if Node.js is installed
Write-Host "`n📦 Checking Node.js installation..." -ForegroundColor Yellow
try {
    $nodeVersion = node --version 2>&1
    Write-Host "   Node.js version: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Node.js is not installed. Please install from https://nodejs.org/" -ForegroundColor Red
    exit 1
}

# Check if Azurite is installed
Write-Host "`n📦 Checking Azurite installation..." -ForegroundColor Yellow
$azuriteInstalled = $false
try {
    $azuriteVersion = npm list -g azurite 2>&1
    if ($azuriteVersion -match "azurite@") {
        $azuriteInstalled = $true
        Write-Host "   Azurite is installed" -ForegroundColor Green
    }
} catch {
    # Not installed
}

# Install Azurite if needed
if (-not $azuriteInstalled) {
    Write-Host "   Installing Azurite globally..." -ForegroundColor Yellow
    npm install -g azurite
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   ❌ Failed to install Azurite" -ForegroundColor Red
        exit 1
    }
    Write-Host "   ✅ Azurite installed successfully" -ForegroundColor Green
}

# Create data directory
Write-Host "`n📁 Setting up data directory..." -ForegroundColor Yellow
$dataPath = Resolve-Path -Path $Location -ErrorAction SilentlyContinue
if (-not $dataPath) {
    New-Item -ItemType Directory -Path $Location -Force | Out-Null
    $dataPath = Resolve-Path -Path $Location
}
Write-Host "   Data directory: $dataPath" -ForegroundColor Green

# Check if Azurite is already running
Write-Host "`n🔍 Checking for running Azurite instance..." -ForegroundColor Yellow
$existingProcess = Get-Process -Name "node" -ErrorAction SilentlyContinue | 
    Where-Object { $_.CommandLine -match "azurite" }

if ($existingProcess) {
    Write-Host "   ⚠️ Azurite appears to be already running (PID: $($existingProcess.Id))" -ForegroundColor Yellow
    $response = Read-Host "   Do you want to restart it? (y/N)"
    if ($response -eq "y" -or $response -eq "Y") {
        Stop-Process -Id $existingProcess.Id -Force
        Start-Sleep -Seconds 2
    } else {
        Write-Host "`n✅ Azurite is already running. Ready for development!" -ForegroundColor Green
        exit 0
    }
}

# Start Azurite
Write-Host "`n🚀 Starting Azurite..." -ForegroundColor Yellow

$azuriteArgs = @(
    "--tablePort", $Port,
    "--location", $dataPath
)

if ($Silent) {
    $azuriteArgs += "--silent"
}

Write-Host "   Connection string: UseDevelopmentStorage=true" -ForegroundColor Cyan
Write-Host "   Table Storage port: $Port" -ForegroundColor Cyan

# Start Azurite in background
Start-Process -FilePath "azurite" -ArgumentList $azuriteArgs -NoNewWindow

Start-Sleep -Seconds 2

# Verify it's running
try {
    $tcpConnection = Test-NetConnection -ComputerName "127.0.0.1" -Port $Port -WarningAction SilentlyContinue
    if ($tcpConnection.TcpTestSucceeded) {
        Write-Host "`n✅ Azurite is running and ready!" -ForegroundColor Green
        Write-Host "`n📋 Next steps:" -ForegroundColor Cyan
        Write-Host "   1. Run 'dotnet run' from src/PoNovaWeight.Api" -ForegroundColor White
        Write-Host "   2. Run 'dotnet test' to execute integration tests" -ForegroundColor White
    } else {
        Write-Host "`n⚠️ Azurite started but port $Port is not responding" -ForegroundColor Yellow
    }
} catch {
    Write-Host "`n⚠️ Could not verify Azurite status" -ForegroundColor Yellow
}

Write-Host "`n" -NoNewline
