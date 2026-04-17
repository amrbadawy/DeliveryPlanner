param(
    [switch]$KillOnly,
    [switch]$RunOnly
)

$ErrorActionPreference = "SilentlyContinue"

# Adjust if your BE/FE use different ports
$PortsToKill = @(2026, 7018, 3000, 5173, 4200)

function Stop-ByPort {
    param([int[]]$Ports)

    foreach ($port in $Ports) {
        $pids = Get-NetTCPConnection -LocalPort $port -State Listen |
            Select-Object -ExpandProperty OwningProcess -Unique

        foreach ($pid in $pids) {
            if ($pid -and $pid -ne 0) {
                Write-Host "Stopping PID $pid on port $port"
                Stop-Process -Id $pid -Force
            }
        }
    }
}

function Start-Portal {
    $projectPath = Join-Path $PSScriptRoot "..\SoftwareDeliveryPlanner.Web\SoftwareDeliveryPlanner.Web.csproj"
    Write-Host "Starting Portal (Blazor Web)..."
    dotnet run --project $projectPath --launch-profile https
}

if (-not $RunOnly) {
    Write-Host "Killing running BE/FE processes by common dev ports..."
    Stop-ByPort -Ports $PortsToKill
}

if (-not $KillOnly) {
    Start-Portal
}
