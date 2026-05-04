namespace GhCLI.Protocol;

public sealed class CanvasSummaryRequest
{
    public string Scope { get; set; } = "full";
    public string? DocumentPath { get; set; }
}

public sealed class CanvasSummaryData
{
    public string SessionId { get; set; } = string.Empty;
    public string? DocumentId { get; set; }
    public string? DocumentName { get; set; }
    public string GraphHash { get; set; } = string.Empty;
    public int NodeCount { get; set; }
    public int EdgeCount { get; set; }
    public List<string> SelectedNodeIds { get; set; } = new();
    public List<CanvasNodeSummaryModel> Nodes { get; set; } = new();
    public List<CanvasEdgeSummaryModel> Edges { get; set; } = new();
    public List<object> Groups { get; set; } = new();
    public List<object> ClusterBoundaries { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
