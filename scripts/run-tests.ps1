# SmartGym Automated Test Runner (PowerShell)
Write-Host "=== Running SmartGym Multi-Tier Test Suites ===" -ForegroundColor Cyan

# 1. Backend Tests
Write-Host "`n[1/3] Running Backend Tests (.NET 8 xUnit)..." -ForegroundColor Yellow
dotnet test SmartGym.sln --verbosity normal
$backendExit = $LASTEXITCODE

# 2. AI Microservice Tests
Write-Host "`n[2/3] Running AI Microservice Tests (pytest)..." -ForegroundColor Yellow
python -m pytest tests/ai
$aiExit = $LASTEXITCODE

# 3. React Frontend Build Check
Write-Host "`n[3/3] Verifying React Frontend Build..." -ForegroundColor Yellow
Push-Location "frontend-web/smartgym-web"
npm run build
$reactExit = $LASTEXITCODE
Pop-Location

Write-Host "`n=== Test Summary ===" -ForegroundColor Cyan
Write-Host "Backend Tests: " -NoNewline; if ($backendExit -eq 0) { Write-Host "PASSED" -ForegroundColor Green } else { Write-Host "FAILED" -ForegroundColor Red }
Write-Host "AI Tests:      " -NoNewline; if ($aiExit -eq 0) { Write-Host "PASSED" -ForegroundColor Green } else { Write-Host "FAILED" -ForegroundColor Red }
Write-Host "React Build:   " -NoNewline; if ($reactExit -eq 0) { Write-Host "PASSED" -ForegroundColor Green } else { Write-Host "FAILED" -ForegroundColor Red }

if ($backendExit -ne 0 -or $aiExit -ne 0 -or $reactExit -ne 0) {
    exit 1
}
