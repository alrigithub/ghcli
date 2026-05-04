using System.Text.Json;
using GhCLI.Protocol;

namespace GhCLI;

internal static class Program
{
    private const string DefaultPipeName = "ghcli.v1";
    private const int DefaultTimeoutMs = 5000;

    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            WriteValidationFailure("A command is required.");
            return 3;
        }

        RpcRequestEnvelope request;
        string pipeName;
        int timeoutMs;

        try
        {
            var parsed = CliArguments.Parse(args);
            if (!IsKnownCommand(parsed.Command))
            {
                throw new ArgumentException($"Unsupported command: {parsed.Command}");
            }

            pipeName = GetOption(parsed.Options, "pipe", DefaultPipeName);
            timeoutMs = int.TryParse(GetOption(parsed.Options, "timeout-ms", DefaultTimeoutMs.ToString()), out var ms)
                ? ms
                : DefaultTimeoutMs;
            if (timeoutMs <= 0)
            {
                throw new ArgumentException("--timeout-ms must be greater than 0.");
            }

            var payload = BuildParams(parsed);
            request = new RpcRequestEnvelope
            {
                Id = Guid.NewGuid().ToString("N"),
                Command = parsed.Command,
                Params = payload
            };
        }
        catch (Exception ex)
        {
            WriteValidationFailure(ex.Message);
            return 3;
        }

        var requestJson = JsonSerializer.Serialize(request, ProtocolJson.Options);

        string responseJson;
        try
        {
            responseJson = await NamedPipeTransport
                .SendAsync(pipeName, requestJson, timeoutMs, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var message = ex is OperationCanceledException
                ? $"Timed out communicating with named pipe '{pipeName}' within {timeoutMs}ms."
                : ex.Message;
            var error = RpcResponse.Failure(request.Command, "plugin_unavailable", message, request.Id);
            Console.Out.WriteLine(JsonSerializer.Serialize(error, ProtocolJson.Options));
            return 2;
        }

        Console.Out.WriteLine(responseJson);

        try
        {
            var response = JsonSerializer.Deserialize<RpcResponseEnvelope>(responseJson, ProtocolJson.Options);
            if (response?.Ok == true)
            {
                return 0;
            }

            return response?.Error?.Code switch
            {
                "plugin_unavailable" => 2,
                "validation" => 3,
                "solve_timeout" => 4,
                _ => 1
            };
        }
        catch
        {
            return 1;
        }
    }

    private static bool IsKnownCommand(string command)
    {
        return command is CommandNames.Status
            or CommandNames.CanvasSummary
            or CommandNames.NodeRead
            or CommandNames.TxnApply
            or CommandNames.GraphApply
            or CommandNames.SolveRun
            or CommandNames.DebugRead;
    }

    private static void WriteValidationFailure(string message)
    {
        var error = RpcResponse.Failure("cli", "validation", message);
        Console.Out.WriteLine(JsonSerializer.Serialize(error, ProtocolJson.Options));
    }

    private static JsonElement? BuildParams(CliArguments args)
    {
        if (args.Options.TryGetValue("json", out var inlineJson) && !string.IsNullOrWhiteSpace(inlineJson))
        {
            using var doc = JsonDocument.Parse(inlineJson);
            return doc.RootElement.Clone();
        }

        if (args.Options.TryGetValue("payload-file", out var payloadFile) && !string.IsNullOrWhiteSpace(payloadFile))
        {
            return ReadJsonFile(payloadFile);
        }

        return args.Command switch
        {
            CommandNames.Status => null,
            CommandNames.CanvasSummary => ToElement(new CanvasSummaryRequest
            {
                Scope = GetOption(args.Options, "scope", "full"),
                DocumentPath = GetOption(args.Options, "document-path")
            }),
            CommandNames.NodeRead => ToElement(new NodeReadRequest
            {
                NodeId = GetOption(args.Options, "node-id"),
                ComponentId = GetOption(args.Options, "component-id"),
                IncludeSource = GetBoolOption(args.Options, "include-source")
            }),
            CommandNames.TxnApply => BuildTransactionPayload(args.Options),
            CommandNames.GraphApply => BuildJsonPayload(args.Options, "graph.apply requires --file <graph.json> or --json '<payload>'."),
            CommandNames.SolveRun => ToElement(new SolveRunRequest
            {
                NodeId = GetOption(args.Options, "node-id")
            }),
            CommandNames.DebugRead => ToElement(new DebugReadRequest
            {
                NodeId = GetOption(args.Options, "node-id")
            }),
            _ => null
        };
    }

    private static JsonElement? BuildTransactionPayload(Dictionary<string, string?> options)
    {
        return BuildJsonPayload(options, "txn.apply requires --file <transaction.json> or --json '<payload>'.");
    }

    private static JsonElement? BuildJsonPayload(Dictionary<string, string?> options, string missingMessage)
    {
        var file = GetOption(options, "file");
        if (!string.IsNullOrWhiteSpace(file))
        {
            return ReadJsonFile(file);
        }

        var inlineJson = GetOption(options, "json");
        if (!string.IsNullOrWhiteSpace(inlineJson))
        {
            using var doc = JsonDocument.Parse(inlineJson);
            return doc.RootElement.Clone();
        }

        throw new ArgumentException(missingMessage);
    }

    private static JsonElement ReadJsonFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"JSON payload file not found: {path}", path);
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.Clone();
    }

    private static JsonElement ToElement<T>(T value)
    {
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(value, ProtocolJson.Options));
        return doc.RootElement.Clone();
    }

    private static bool GetBoolOption(Dictionary<string, string?> options, string key)
    {
        var raw = GetOption(options, key);
        return string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase)
               || string.Equals(raw, "1", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetOption(Dictionary<string, string?> options, string key)
    {
        return options.TryGetValue(key, out var value) ? value : null;
    }

    private static string GetOption(Dictionary<string, string?> options, string key, string defaultValue)
    {
        return options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : defaultValue;
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  GhCLI status [--pipe <name>]");
        Console.Error.WriteLine("  GhCLI canvas.summary [--scope full|selected]");
        Console.Error.WriteLine("  GhCLI node.read --node-id <id> [--include-source true]");
        Console.Error.WriteLine("  GhCLI txn.apply --file <transaction.json>");
        Console.Error.WriteLine("  GhCLI graph.apply --file <graph.json>");
        Console.Error.WriteLine("  GhCLI solve.run [--node-id <id>]");
        Console.Error.WriteLine("  GhCLI debug.read --node-id <id>");
    }
}
