using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using OpenUtau.Core;
using OpenUtau.Core.Editing;
using OpenUtau.Core.Ustx;
using OpenUtauDRPC;
using Avalonia.Controls;
using OpenUtauDRPC.Views;

public class DiscordPlugin : BatchEdit {
    public string Name => "Discord Plugin Settings";

    public void Run(UProject project, UVoicePart part, List<UNote> selectedNotes, DocManager docManager) {
        var discordPref = new PreferencesDialog();
        discordPref.Show();
    }

    [ModuleInitializer]
    internal static void Initialize()
    {
        string hookPath = Assembly.GetExecutingAssembly().Location;
        var resolver = new AssemblyDependencyResolver(hookPath);
        AssemblyLoadContext.Default.Resolving += (context, assemblyName) => {
            string? assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath != null ? context.LoadFromAssemblyPath(assemblyPath) : null;
        };

        DocManager.Inst.AddSubscriber(new UDiscordRPC());
    }
}