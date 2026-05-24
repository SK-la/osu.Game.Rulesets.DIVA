# 检测 nuget.org 上官方 ppy.osu.* 最新稳定版并更新 Dependencies.props
param(
    [string]$PropsPath = 'Dependencies.props'
)

$ErrorActionPreference = 'Stop'

function Get-LatestStableNuGetVersion {
    param([Parameter(Mandatory = $true)][string]$PackageId)

    $url = "https://api.nuget.org/v3-flatcontainer/$($PackageId.ToLowerInvariant())/index.json"
    $index = Invoke-RestMethod -Uri $url -TimeoutSec 30
    if (-not $index.versions -or $index.versions.Count -eq 0) {
        throw "nuget.org 上未找到包: $PackageId"
    }

    # 排除 -tachyon、-test、-lazer 等预发布后缀，与 osu! lazer 稳定包一致
    $stable = $index.versions | Where-Object { $_ -notmatch '-' }
    if (-not $stable) {
        throw "包 $PackageId 没有稳定版本"
    }

    return $stable[-1]
}

$packageMap = [ordered]@{
    'ppy.osu.game'              = 'PpyOsuGameVersion'
    'ppy.osu.game.rulesets.osu' = 'PpyOsuGameRulesetsOsuVersion'
}

if (-not (Test-Path $PropsPath)) {
    throw "找不到 props 文件: $PropsPath"
}

$content = Get-Content -Path $PropsPath -Raw
$changes = @()

foreach ($entry in $packageMap.GetEnumerator()) {
    $packageId = $entry.Key
    $propertyName = $entry.Value
    $latest = Get-LatestStableNuGetVersion -PackageId $packageId
    $pattern = "<$propertyName>([^<]*)</$propertyName>"
    $match = [regex]::Match($content, $pattern)
    if (-not $match.Success) {
        throw "props 中缺少元素 <$propertyName>"
    }

    $current = $match.Groups[1].Value
    if ($current -eq $latest) {
        Write-Host "unchanged: $propertyName = $latest"
        continue
    }

    $content = [regex]::Replace($content, $pattern, "<$propertyName>$latest</$propertyName>")
    $changes += "$propertyName`: $current -> $latest"
    Write-Host "updated: $propertyName = $latest (was $current)"
}

if ($changes.Count -eq 0) {
    Write-Host 'No dependency updates required.'
    if ($env:GITHUB_OUTPUT) {
        'changed=false' | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
    }
    exit 0
}

Set-Content -Path $PropsPath -Value $content -Encoding utf8 -NoNewline
if ($env:GITHUB_OUTPUT) {
    'changed=true' | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
    "summary=$($changes -join '; ')" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
}
