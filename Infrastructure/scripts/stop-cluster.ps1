# ============================================================
# Stop EbayClone API Cluster
# ============================================================
#
# Usage:
#   .\stop-cluster.ps1       → Stop all instances started by start-cluster.ps1
#   .\stop-cluster.ps1 -All  → Kill ALL dotnet processes (careful!)
# ============================================================

param(
    [switch]$All
)

$pidsFile = Join-Path $PSScriptRoot "cluster-pids.txt"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " Stopping EbayClone API Cluster" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

if ($All) {
    # Kill tất cả dotnet processes
    Write-Host "[WARN] Killing ALL dotnet processes..." -ForegroundColor Yellow
    $procs = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
    foreach ($proc in $procs) {
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        Write-Host "  [OK] Killed PID=$($proc.Id)" -ForegroundColor Green
    }
} elseif (Test-Path $pidsFile) {
    # Kill chỉ các instances đã start bởi start-cluster.ps1
    $pids = Get-Content $pidsFile
    $stoppedCount = 0
    foreach ($procId in $pids) {
        $procId = $procId.Trim()
        if ($procId -and $procId -match '^\d+$') {
            try {
                $proc = Get-Process -Id ([int]$procId) -ErrorAction SilentlyContinue
                if ($proc) {
                    Stop-Process -Id ([int]$procId) -Force
                    Write-Host "  [OK] Stopped PID=$procId" -ForegroundColor Green
                    $stoppedCount++
                } else {
                    Write-Host "  [SKIP] PID=$procId already stopped" -ForegroundColor Gray
                }
            } catch {
                Write-Host "  [SKIP] PID=$procId not found (already exited)" -ForegroundColor Gray
            }
        }
    }
    
    Remove-Item $pidsFile -Force
    Write-Host ""
    Write-Host "[OK] Stopped $stoppedCount instance(s). PID file cleaned up." -ForegroundColor Green
} else {
    Write-Host "[WARN] No cluster-pids.txt found." -ForegroundColor Yellow
    Write-Host "   Use -All flag to kill all dotnet processes: .\stop-cluster.ps1 -All" -ForegroundColor Yellow
}

Write-Host ""
