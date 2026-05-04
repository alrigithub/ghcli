namespace GhCLI.Protocol;

public sealed class PositionModel
{
    public double X { get; set; }
    public double Y { get; set; }
}

public sealed class PortSummaryModel
{
    public string Name { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string? TypeName { get; set; }
    public string? TypeHint { get; set; }
    public string Access { get; set; } = "item";
    public bool Connected { get; set; }
}

public sealed class PythonSummaryModel
{
    public string Runtime { get; set; } = "cpython3";
    public string? SourceHash { get; set; }
    public int InputCount { get; set; }
    public int OutputCount { get; set; }
}

public sealed class NodeFlagsModel
{
    public bool Selected { get; set; }
    public bool Locked { get; set; }
    public bool Hidden { get; set; }
}

public sealed class CanvasNodeSummaryModel
{
    public string Id { get; set; } = string.Empty;
    public string ComponentId { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string Kind { get; set; } = "unknown";
    public string TypeName { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string DocumentPath { get; set; } = "root";
    public PositionModel Position { get; set; } = new();
    public List<PortSummaryModel> Inputs { get; set; } = new();
    public List<PortSummaryModel> Outputs { get; set; } = new();
    public PythonSummaryModel? Python { get; set; }
    public NodeFlagsModel Flags { get; set; } = new();
}

public sealed class CanvasEdgeSummaryModel
{
    public string SourceNodeId { get; set; } = string.Empty;
    public string SourcePort { get; set; } = string.Empty;
    public string TargetNodeId { get; set; } = string.Empty;
    public string TargetPort { get; set; } = string.Empty;
}

public sealed class RuntimeMessageModel
{
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class OutputSummaryModel
{
    public string Port { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public int BranchCount { get; set; }
    public List<string> Preview { get; set; } = new();
}

public sealed class ActiveDocumentModel
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? FilePath { get; set; }
}

public sealed class VariableParamsModel
{
    public bool SupportsVariableParams { get; set; }
    public int InputCount { get; set; }
    public int OutputCount { get; set; }
    public List<string> InputNames { get; set; } = new();
    public List<string> OutputNames { get; set; } = new();
}
