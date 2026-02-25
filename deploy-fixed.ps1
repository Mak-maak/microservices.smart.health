# ====================================================================
# SmartHealth Appointments API - Fixed Deployment Script
# ====================================================================

$ErrorActionPreference = "Continue"
$projectRoot = "C:\Users\HP\source\repos\microservices.smart.health"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "🚀 Starting Deployment with Health Checks" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

Set-Location $projectRoot

# Stop and rebuild
Write-Host "🛑 Stopping containers..." -ForegroundColor Yellow
docker compose down

Write-Host "`n🔨 Building project..." -ForegroundColor Yellow
dotnet build --configuration Release

Write-Host "`n🐳 Rebuilding Docker images..." -ForegroundColor Yellow
docker compose build --no-cache

Write-Host "`n🚀 Starting containers..." -ForegroundColor Yellow
docker compose up -d

# Wait for health checks
Write-Host "`n⏳ Waiting for health checks (60 seconds)..." -ForegroundColor Yellow
Start-Sleep -Seconds 60

# Check status
Write-Host "`n📊 Container Status:" -ForegroundColor Cyan
docker compose ps

Write-Host "`n🧪 Testing Endpoints..." -ForegroundColor Cyan
try {
    curl http://localhost:8080/liveness
    curl http://localhost:8080/health
    Write-Host "`n✅ API is responding!" -ForegroundColor Green
} catch {
    Write-Host "`n⚠️ API not responding yet" -ForegroundColor Yellow
}

Write-Host "`n📋 Recent logs:" -ForegroundColor Cyan
docker compose logs api --tail=30

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "✅ Deployment Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "Run: .\test-e2e.ps1 to test the saga flow" -ForegroundColor White
Write-Host ""
