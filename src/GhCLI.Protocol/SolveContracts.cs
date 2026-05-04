namespace GhCLI.Protocol;

public sealed class SolveRunRequest
{
    public string? NodeId { get; set; }
}

public sealed class SolveRunData
{
    public bool Success { get; set; }
    public double SolveMs { get; set; }
    public List<RuntimeMessageModel> RuntimeMessages { get; set; } = new();
}
