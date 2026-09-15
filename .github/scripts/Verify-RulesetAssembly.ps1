# 校验 ruleset DLL 可被反射加载（捕获缺失源生成器实现的 ReflectionTypeLoadException）
param(
    [Parameter(Mandatory = $true)]
    [string]$AssemblyPath
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $AssemblyPath)) {
    throw "Assembly not found: $AssemblyPath"
}

$assemblyDirectory = Split-Path -Parent (Resolve-Path $AssemblyPath).Path

Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Linq;
using System.Reflection;

public static class RulesetAssemblyVerifier
{
    public static void Verify(string path, string dependencyDirectory)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            var name = new AssemblyName(args.Name).Name;
            if (name == null)
                return null;

            var candidate = Path.Combine(dependencyDirectory, name + ".dll");
            return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
        };

        var assembly = Assembly.LoadFrom(path);
        try
        {
            _ = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            var missingImplementation = ex.LoaderExceptions
                .Where(e => e != null && e.Message.Contains("does not have an implementation", StringComparison.Ordinal))
                .Select(e => e!.Message)
                .ToArray();

            if (missingImplementation.Length > 0)
            {
                throw new InvalidOperationException(
                    "Ruleset assembly failed type load (likely missing framework source generator output):" + Environment.NewLine +
                    string.Join(Environment.NewLine, missingImplementation),
                    ex);
            }

            throw;
        }
    }
}
'@

[RulesetAssemblyVerifier]::Verify((Resolve-Path $AssemblyPath).Path, $assemblyDirectory)
Write-Host "Ruleset assembly verified: $AssemblyPath"
