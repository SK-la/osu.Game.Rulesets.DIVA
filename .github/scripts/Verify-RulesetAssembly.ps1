# 校验 ruleset DLL 可被反射加载（捕获缺失源生成器实现的 ReflectionTypeLoadException）
param(
    [Parameter(Mandatory = $true)]
    [string]$AssemblyPath
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $AssemblyPath)) {
    throw "Assembly not found: $AssemblyPath"
}

$resolvedAssemblyPath = (Resolve-Path $AssemblyPath).Path

# PowerShell 可能仍运行于旧版 .NET；将反射校验交给 net11 宿主，避免在 pwsh 的 CLR 中加载规则集。
& dotnet run --file (Join-Path $PSScriptRoot 'Verify-RulesetAssembly.cs') -- $resolvedAssemblyPath
if ($LASTEXITCODE -ne 0) {
    throw "Ruleset assembly verification failed: $resolvedAssemblyPath"
}

Write-Host "Ruleset assembly verified: $AssemblyPath"
