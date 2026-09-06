// 在匹配 TFM 的 dotnet 宿主中反射加载 ruleset，避免 pwsh 宿主无法解析 System.Runtime 10。
using System.Reflection;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: VerifyRulesetAssembly <assembly-path>");
    return 1;
}

var path = Path.GetFullPath(args[0]);
if (!File.Exists(path))
{
    Console.Error.WriteLine($"Assembly not found: {path}");
    return 1;
}

var dependencyDirectory = Path.GetDirectoryName(path)!;

AppDomain.CurrentDomain.AssemblyResolve += (_, resolveArgs) =>
{
    var name = new AssemblyName(resolveArgs.Name).Name;
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
        Console.Error.WriteLine(
            "Ruleset assembly failed type load (likely missing framework source generator output):");
        foreach (var message in missingImplementation)
            Console.Error.WriteLine(message);
        return 1;
    }

    Console.Error.WriteLine(ex);
    foreach (var loader in ex.LoaderExceptions)
    {
        if (loader != null)
            Console.Error.WriteLine(loader.Message);
    }

    return 1;
}

Console.WriteLine($"Ruleset assembly verified: {path}");
return 0;
