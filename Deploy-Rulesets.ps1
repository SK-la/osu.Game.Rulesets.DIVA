#Requires -Version 5.1
<#
.SYNOPSIS
  Release-build DIVA ruleset for net8 + net10 and copy DLLs into local clients.

.DESCRIPTION
  - net10 → F:\MUG OSU\EZ2OSU-lazer\rulesets\osu.Game.Rulesets.Diva.dll
  - net8  → F:\MUG OSU\osu-lazer\rulesets\osu.Game.Rulesets.Diva.dll
  Existing files are overwritten. Only the ruleset DLL is copied (not Game/Framework deps).
  If the destination DLL is locked, processes launched from that client folder are stopped, then copy is retried.
#>

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $repoRoot 'osu.Game.Rulesets.Diva\osu.Game.Rulesets.Diva.csproj'

$targets = @(
    @{
        Framework      = 'net10.0'
        DestinationDir = 'F:\MUG OSU\EZ2OSU-lazer\rulesets'
        ClientRoot     = 'F:\MUG OSU\EZ2OSU-lazer'
        Label          = 'Ez2Lazer'
    },
    @{
        Framework      = 'net8.0'
        DestinationDir = 'F:\MUG OSU\osu-lazer\rulesets'
        ClientRoot     = 'F:\MUG OSU\osu-lazer'
        Label          = 'osu!lazer'
    }
)

function Get-ClientProcesses {
    param([Parameter(Mandatory)][string]$ClientRoot)

    $root = [System.IO.Path]::GetFullPath($ClientRoot).TrimEnd('\', '/')
    $prefix = $root + [System.IO.Path]::DirectorySeparatorChar

    Get-CimInstance Win32_Process | Where-Object {
        if ([string]::IsNullOrWhiteSpace($_.ExecutablePath)) {
            return $false
        }

        try {
            $exe = [System.IO.Path]::GetFullPath($_.ExecutablePath)
            return $exe.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase) -or
                   $exe.Equals($root, [System.StringComparison]::OrdinalIgnoreCase)
        }
        catch {
            return $false
        }
    }
}

function Stop-ClientProcesses {
    param([Parameter(Mandatory)][string]$ClientRoot)

    $procs = @(Get-ClientProcesses -ClientRoot $ClientRoot)
    if ($procs.Count -eq 0) {
        return $false
    }

    foreach ($p in $procs) {
        Write-Host ("    Stopping {0} (PID {1}) to unlock ruleset DLL..." -f $p.Name, $p.ProcessId) -ForegroundColor Yellow
        Stop-Process -Id $p.ProcessId -Force -ErrorAction SilentlyContinue
    }

    Start-Sleep -Milliseconds 800
    return $true
}

function Copy-RulesetDll {
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][string]$Destination,
        [Parameter(Mandatory)][string]$ClientRoot,
        [int]$Retries = 5
    )

    for ($attempt = 1; $attempt -le $Retries; $attempt++) {
        try {
            Copy-Item -LiteralPath $Source -Destination $Destination -Force
            return
        }
        catch [System.IO.IOException], [System.UnauthorizedAccessException] {
            Write-Host ("    Copy locked (attempt {0}/{1}): {2}" -f $attempt, $Retries, $_.Exception.Message) -ForegroundColor Yellow

            $stopped = Stop-ClientProcesses -ClientRoot $ClientRoot
            if (-not $stopped -and $attempt -eq $Retries) {
                throw ("Cannot overwrite '{0}'. Close the game (or any process locking the DLL) and re-run.`n{1}" -f $Destination, $_.Exception.Message)
            }

            Start-Sleep -Milliseconds (400 * $attempt)
        }
    }

    throw "Failed to copy '$Source' to '$Destination' after $Retries attempts."
}

if (-not (Test-Path -LiteralPath $project)) {
    throw "Project not found: $project"
}

Push-Location $repoRoot
try {
    foreach ($t in $targets) {
        Write-Host ""
        Write-Host "==> Publish $($t.Framework) ($($t.Label))" -ForegroundColor Cyan

        & dotnet publish $project -c Release -f $t.Framework --nologo
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet publish failed for $($t.Framework) (exit $LASTEXITCODE)"
        }

        $sourceDll = Join-Path $repoRoot "osu.Game.Rulesets.Diva\bin\Release\$($t.Framework)\publish\osu.Game.Rulesets.Diva.dll"
        if (-not (Test-Path -LiteralPath $sourceDll)) {
            throw "Publish output missing: $sourceDll"
        }

        if (-not (Test-Path -LiteralPath $t.DestinationDir)) {
            New-Item -ItemType Directory -Path $t.DestinationDir -Force | Out-Null
        }

        $destDll = Join-Path $t.DestinationDir 'osu.Game.Rulesets.Diva.dll'

        # Proactively stop client if already running under this install.
        [void](Stop-ClientProcesses -ClientRoot $t.ClientRoot)
        Copy-RulesetDll -Source $sourceDll -Destination $destDll -ClientRoot $t.ClientRoot

        $info = Get-Item -LiteralPath $destDll
        Write-Host ("    OK  {0}  ({1:N0} bytes, {2:yyyy-MM-dd HH:mm:ss})" -f $destDll, $info.Length, $info.LastWriteTime) -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "Done. Start the client(s) again to load the updated ruleset." -ForegroundColor Green
}
finally {
    Pop-Location
}
