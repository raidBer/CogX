# ================================================================================
# CogX Database Reset Script
# ================================================================================
# This script drops and recreates the CogX database
# 
# Usage: Run this script from the solution directory
# .\reset-database.ps1
# ================================================================================

Write-Host ""
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "  CogX Database Reset Utility" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host ""

# Stop on first error
$ErrorActionPreference = "Stop"

try {
    # Step 1: Drop the database
    Write-Host "[1/2] " -NoNewline -ForegroundColor Yellow
    Write-Host "Dropping database CogXDb..." -ForegroundColor White
    
    dotnet ef database drop --force --project CogX\CogX.csproj
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "      ? Database dropped successfully" -ForegroundColor Green
    } else {
        Write-Host "      ? Failed to drop database" -ForegroundColor Red
        exit 1
    }
    
    Write-Host ""
    
    # Step 2: Recreate the database
    Write-Host "[2/2] " -NoNewline -ForegroundColor Yellow
    Write-Host "Creating database from migrations..." -ForegroundColor White
    
    dotnet ef database update --project CogX\CogX.csproj
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "      ? Database created successfully" -ForegroundColor Green
    } else {
        Write-Host "      ? Failed to create database" -ForegroundColor Red
        exit 1
    }
    
    Write-Host ""
    Write-Host "================================================================================" -ForegroundColor Green
    Write-Host "  ? Database reset completed successfully!" -ForegroundColor Green
    Write-Host "================================================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "The CogXDb database has been reset and is ready to use." -ForegroundColor White
    Write-Host ""
    
} catch {
    Write-Host ""
    Write-Host "================================================================================" -ForegroundColor Red
    Write-Host "  ? Error occurred during database reset" -ForegroundColor Red
    Write-Host "================================================================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error details: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting tips:" -ForegroundColor Yellow
    Write-Host "  1. Make sure the backend application is stopped (not running in Visual Studio)" -ForegroundColor White
    Write-Host "  2. Close any SQL Server Object Explorer connections to the database" -ForegroundColor White
    Write-Host "  3. Try running 'dotnet build' first to ensure the project compiles" -ForegroundColor White
    Write-Host ""
    exit 1
}
