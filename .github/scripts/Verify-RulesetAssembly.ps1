# 校验 ruleset DLL 可被反射加载（捕获缺失源生成器实现的 ReflectionTypeLoadException）
# 使用匹配 TFM 的 dotnet 宿主运行，避免 pwsh/.NET 8 宿主无法加载 net10 程序集。
param(
    [Parameter(Mandatory = $true)]
    [string]$AssemblyPath
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $AssemblyPath)) {
    throw "Assembly not found: $AssemblyPath"
}

$resolved = (Resolve-Path $AssemblyPath).Path
$tfm = 'net8.0'
if ($resolved -match '[\\/](net[0-9]+(?:\.[0-9]+)?)[\\/]') {
    $tfm = $Matches[1]
}

$verifyProject = Join-Path $PSScriptRoot 'VerifyRulesetAssembly/VerifyRulesetAssembly.csproj'
dotnet run --project $verifyProject -f $tfm -c Release --no-launch-profile -- $resolved
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
