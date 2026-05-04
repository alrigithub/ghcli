using System.Text.Json;

namespace GhCLI.Protocol;

public sealed class RpcRequestEnvelope
{
    public string? Id { get; set; }
    public string Command { get; set; } = string.Empty;
    public JsonElement? Params { get; set; }
}

public sealed class RpcResponseEnvelope
{
    public bool Ok { get; set; }
    public string? Id { get; set; }
    public string Command { get; set; } = string.Empty;
    public object? Data { get; set; }
    public List<string> Warnings { get; set; } = new();
    public RpcError? Error { get; set; }
}

public sealed class RpcError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public static class RpcResponse
{
    public static RpcResponseEnvelope Success(
        string command,
        object? data,
        string? id = null,
        IEnumerable<string>? warnings = null)
    {
        return new RpcResponseEnvelope
        {
            Ok = true,
            Id = id,
            Command = command,
            Data = data,
            Warnings = warnings?.ToList() ?? new List<string>()
        };
    }

    public static RpcResponseEnvelope Failure(
        string command,
        string code,
        string message,
        string? id = null,
        IEnumerable<string>? warnings = null)
    {
        return new RpcResponseEnvelope
        {
            Ok = false,
            Id = id,
            Command = command,
            Warnings = warnings?.ToList() ?? new List<string>(),
            Error = new RpcError
            {
                Code = code,
                Message = message
            }
        };
    }
}
