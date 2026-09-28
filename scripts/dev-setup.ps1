# SmartGym Development Environment Setup Script (PowerShell)
Write-Host "=== Setting up SmartGym Development Environment ===" -ForegroundColor Cyan

# 1. Check & Copy .env
if (-not (Test-Path ".env")) {
    Copy-Item ".env.example" ".env"
    Write-Host "[OK] Created .env from .env.example" -ForegroundColor Green
} else {
    Write-Host "[INFO] .env already exists" -ForegroundColor Yellow
}

# 2. Restore .NET Solution
Write-Host "Restoring .NET dependencies..." -ForegroundColor Cyan
dotnet restore SmartGym.sln

# 3. Python AI Dependencies
Write-Host "Installing Python AI dependencies..." -ForegroundColor Cyan
pip install -r ai-service/requirements.txt

# 4. Frontend Web Dependencies
Write-Host "Installing React frontend dependencies..." -ForegroundColor Cyan
Set-Location "frontend-web/smartgym-web"
npm install
Set-Location "../.."

Write-Host "=== SmartGym Development Setup Complete! ===" -ForegroundColor Green
