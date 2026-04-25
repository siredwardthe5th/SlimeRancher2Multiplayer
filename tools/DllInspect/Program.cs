using System.Reflection;

// Walk up from the binary location until we find SR2MP/libraries
static string FindLibrariesFolder()
{
    var dir = AppContext.BaseDirectory;
    while (true)
    {
        var candidate = Path.Combine(dir, "SR2MP", "libraries");
        if (Directory.Exists(candidate))
            return candidate;
        var parent = Path.GetDirectoryName(dir);
        if (parent == null || parent == dir)
            throw new DirectoryNotFoundException(
                "Could not find SR2MP/libraries. Run from within the repo and make sure the libraries folder is populated.");
        dir = parent;
    }
}

var libDir = FindLibrariesFolder();
var dllPaths = Directory.GetFiles(libDir, "*.dll").ToList();
dllPaths.AddRange(Directory.GetFiles(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"));

var mlc = new MetadataLoadContext(new PathAssemblyResolver(dllPaths));

// Usage: edit the query below, then run with `dotnet run` from this folder.
// The Assembly-CSharp.dll in SR2MP/libraries contains all game types.
try
{
    var a = mlc.LoadFromAssemblyPath(Path.Combine(libDir, "Assembly-CSharp.dll"));

    // Example: dump all public members of a type by name
    var typeName = args.Length > 0 ? args[0] : null;
    foreach (var t in a.GetTypes())
    {
        if (typeName != null && !t.Name.Contains(typeName, StringComparison.OrdinalIgnoreCase))
            continue;
        if (typeName == null)
            continue; // no-arg mode: don't dump everything

        Console.WriteLine($"\n=== {t.FullName} ===");
        Console.WriteLine("Properties:");
        foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            Console.WriteLine($"  {p.PropertyType.Name} {p.Name}");
        Console.WriteLine("Methods:");
        foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            Console.WriteLine($"  [{(m.IsPublic ? "pub" : "prv")}] {m.ReturnType.Name} {m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name))})");
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
}
finally
{
    mlc.Dispose();
}
