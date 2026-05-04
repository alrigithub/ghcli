namespace GhCLI.Protocol;

public sealed class DebugReadRequest
{
    public string? NodeId { get; set; }
}

public sealed class DebugReadData
{
    public string? NodeId { get; set; }
    public bool Success { get; set; }
    public List<RuntimeMessageModel> RuntimeMessages { get; set; } = new();
    public List<OutputSummaryModel> OutputSummaries { get; set; } = new();
    public Dictionary<string, string> DebugValues { get; set; } = new();
    public string? SourceHash { get; set; }
    public double SolveMs { get; set; }
}
