# ============================================================
# Start EbayClone API Cluster — 3 Instances
# ============================================================
#
# Usage:
#   .\start-cluster.ps1            → Start 3 instances (default)
#   .\start-cluster.ps1 -Instances 5  → Start 5 instances
#
# [Performance] Multi-instance benefits:
#   - Tận dụng nhiều CPU cores (mỗi instance = 1 process)
#   - Nếu 1 instance crash, Nginx tự chuyển sang instance khác
#   - Zero-downtime deploy: restart từng instance
#
# Yêu cầu:
#   - Redis đang chạy (distributed lock cho background services)
#   - SQL Server đang chạy
#   - Nginx đã cấu hình (nginx.conf)
# ============================================================

param(
    [int]$Instances = 3,
    [int]$StartPort = 5001,
    [string]$Environment = "Development"
)

$projectPath = Join-Path $PSScriptRoot "..\..\Backend\EbayClone.API\EbayClone.API.csproj"
$projectPath = (Resolve-Path $projectPath).Path

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " EbayClone API Cluster -- Starting $Instances instance(s)" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Kiểm tra Redis
Write-Host "[1/3] Checking Redis connection..." -ForegroundColor Yellow
try {
    $redis = New-Object System.Net.Sockets.TcpClient
    $redis.Connect("127.0.0.1", 6380)
    $redis.Close()
    Write-Host "  [OK] Redis is running on port 6380" -ForegroundColor Green
} catch {
    Write-Host "  [WARN] Redis NOT reachable on port 6380!" -ForegroundColor Red
    Write-Host "  -> App will fallback to single-instance mode (background jobs run on all instances)" -ForegroundColor Yellow
    Write-Host "  -> Recommended: docker run -d -p 6380:6379 --name redis redis:7-alpine" -ForegroundColor Yellow
    Write-Host ""
}

# Build project trước
Write-Host "[2/3] Building project..." -ForegroundColor Yellow
$buildResult = dotnet build $projectPath -c Release --nologo -v q 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "  [FAIL] Build FAILED!" -ForegroundColor Red
    Write-Host $buildResult
    exit 1
}
Write-Host "  [OK] Build succeeded" -ForegroundColor Green
Write-Host ""

# Start instances
Write-Host "[3/3] Starting $Instances API instances..." -ForegroundColor Yellow
$pids = @()

for ($i = 0; $i -lt $Instances; $i++) {
    $port = $StartPort + $i
    $url = "http://0.0.0.0:$port"
    
    $proc = Start-Process -FilePath "dotnet" `
        -ArgumentList "run --project `"$projectPath`" --no-build -c Release --urls $url" `
        -PassThru `
        -WindowStyle Minimized `
        -RedirectStandardOutput "NUL"
    
    # Set environment
    $env:ASPNETCORE_ENVIRONMENT = $Environment
    
    $pids += $proc.Id
    Write-Host ("  [OK] Instance {0}: PID={1}, URL={2}" -f ($i + 1), $proc.Id, $url) -ForegroundColor Green
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host " Cluster started successfully!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Instances:" -ForegroundColor White
for ($i = 0; $i -lt $Instances; $i++) {
    $port = $StartPort + $i
    Write-Host ("  -> http://localhost:{0}/health" -f $port) -ForegroundColor Gray
}
Write-Host ""
Write-Host "Nginx proxy: http://localhost (port 80)" -ForegroundColor White
Write-Host ""
Write-Host "To stop all instances:" -ForegroundColor Yellow
Write-Host "  .\stop-cluster.ps1" -ForegroundColor Yellow
Write-Host ""

# Save PIDs to file for stop script
$pids | Out-File -FilePath (Join-Path $PSScriptRoot "cluster-pids.txt") -Encoding ASCII
Write-Host "PIDs saved to cluster-pids.txt" -ForegroundColor Gray
