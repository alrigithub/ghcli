namespace GhCLI.Protocol;

public sealed class NodeReadRequest
{
    public string? NodeId { get; set; }
    public string? ComponentId { get; set; }
    public bool IncludeSource { get; set; }
}

public sealed class NodeReadData
{
    public CanvasNodeSummaryModel? Node { get; set; }
    public List<RuntimeMessageModel> RuntimeMessages { get; set; } = new();
    public VariableParamsModel VariableParams { get; set; } = new();
    public Dictionary<string, object?> ValueState { get; set; } = new();
    public List<OutputSummaryModel> OutputSummaries { get; set; } = new();
    public string? Source { get; set; }
    public string? SourceHash { get; set; }
}
