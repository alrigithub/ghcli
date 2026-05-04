using System.Text.Json;
using System.Text.Json.Serialization;

namespace GhCLI.Protocol;

public sealed class TransactionApplyRequest
{
    public string TransactionId { get; set; } = string.Empty;
    public List<TransactionOperationModel> Operations { get; set; } = new();
    public bool SolveAfter { get; set; }
    public List<string> DebugAfter { get; set; } = new();

    [JsonPropertyName("transaction_id")]
    public string TransactionIdSnake
    {
        get => TransactionId;
        set => TransactionId = value;
    }

    [JsonPropertyName("solve_after")]
    public bool SolveAfterSnake
    {
        get => SolveAfter;
        set => SolveAfter = value;
    }

    [JsonPropertyName("debug_after")]
    public List<string> DebugAfterSnake
    {
        get => DebugAfter;
        set => DebugAfter = value;
    }
}

public sealed class TransactionOperationModel
{
    public string Op { get; set; } = string.Empty;
    public JsonElement Args { get; set; }
}

public sealed class TransactionApplyData
{
    public bool Applied { get; set; }
    public List<string> Created { get; set; } = new();
    public List<string> Patched { get; set; } = new();
    public List<string> Deleted { get; set; } = new();
    public int WireChanges { get; set; }
    public List<string> Warnings { get; set; } = new();
    public string GraphHash { get; set; } = string.Empty;
    public List<DebugReadData> DebugReads { get; set; } = new();
}
