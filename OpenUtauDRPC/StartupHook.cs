using System.Runtime.Loader;
using System.Reflection;
using OpenUtauDRPC;

internal class StartupHook {
    public static void Initialize() {
        string hookPath = Assembly.GetExecutingAssembly().Location;
        var resolver = new AssemblyDependencyResolver(hookPath);
        AssemblyLoadContext.Default.Resolving += (context, assemblyName) => {
            string? assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath != null ? context.LoadFromAssemblyPath(assemblyPath) : null;
        };

        _ = new UDiscordRPC();
    }
}