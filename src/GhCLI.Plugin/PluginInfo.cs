using Grasshopper.Kernel;

namespace GhCLI.Plugin;

public sealed class PluginInfo : GH_AssemblyInfo
{
    public override string Name => "GhCLI";
    public override string Description => "Named-pipe Grasshopper runtime for compact DAG reads and batched writes.";
    public override Guid Id => new("f365b64f-b81c-42e9-b853-27859b6edc0f");
    public override string AuthorName => "cli-ghpython2";
    public override string AuthorContact => "local";
}
