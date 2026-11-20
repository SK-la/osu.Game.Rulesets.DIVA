#:property TargetFramework=net11.0
#:property PublishAot=false

using System;
using System.IO;
using System.Linq;
using System.Reflection;

if (args.Length != 1)
    throw new ArgumentException("Expected the ruleset assembly path.");

var path = Path.GetFullPath(args[0]);
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
        throw new InvalidOperationException(
            "Ruleset assembly failed type load (likely missing framework source generator output):" + Environment.NewLine +
            string.Join(Environment.NewLine, missingImplementation),
            ex);
    }

    throw;
}
