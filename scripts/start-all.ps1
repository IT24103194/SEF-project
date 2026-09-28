# SmartGym Local Multi-Service Launcher (PowerShell)
Write-Host "=== Launching SmartGym Multi-Tier System ===" -ForegroundColor Cyan

Write-Host "Option 1: Launch all containers via Docker Compose" -ForegroundColor Yellow
Write-Host "  docker compose up --build" -ForegroundColor White

Write-Host "`nOption 2: Launch services locally in separate terminals:" -ForegroundColor Yellow
Write-Host "  Terminal 1 (Backend API): cd backend/SmartGym.Api; dotnet run" -ForegroundColor White
Write-Host "  Terminal 2 (AI Service):  cd ai-service; uvicorn smartgym_ai.main:app --port 8000 --reload" -ForegroundColor White
Write-Host "  Terminal 3 (Web Admin):   cd frontend-web/smartgym-web; npm run dev" -ForegroundColor White
Write-Host "  Terminal 4 (Mobile App):  cd mobile/smartgym_mobile; flutter run" -ForegroundColor White
