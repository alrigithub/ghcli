using GhCLI.Plugin.Runtime;
using Grasshopper.Kernel;

namespace GhCLI.Plugin;

public sealed class GhCliAssemblyPriority : GH_AssemblyPriority
{
    public override GH_LoadingInstruction PriorityLoad()
    {
        PluginHost.Start();
        return GH_LoadingInstruction.Proceed;
    }
}
