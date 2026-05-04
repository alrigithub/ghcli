using System.Text.Json;
using GhCLI.Core.Errors;
using GhCLI.Protocol;
using Rhino;

namespace GhCLI.Plugin.Runtime;

internal sealed class CommandRouter
{
    private readonly GrasshopperRuntime _runtime;

    public CommandRouter(GrasshopperRuntime runtime)
    {
        _runtime = runtime;
    }

    public Task<RpcResponseEnvelope> HandleAsync(RpcRequestEnvelope request, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        try
        {
            var response = ExecuteOnUiThread(() => HandleCore(request));
            return Task.FromResult(response);
        }
        catch (CommandValidationException ex)
        {
            return Task.FromResult(RpcResponse.Failure(request.Command, "validation", ex.Message, request.Id));
        }
        catch (PluginUnavailableException ex)
        {
            return Task.FromResult(RpcResponse.Failure(request.Command, "plugin_unavailable", ex.Message, request.Id));
        }
        catch (TimeoutException ex)
        {
            return Task.FromResult(RpcResponse.Failure(request.Command, "solve_timeout", ex.Message, request.Id));
        }
        catch (Exception ex)
        {
            return Task.FromResult(RpcResponse.Failure(request.Command, "runtime_failure", ex.Message, request.Id));
        }
    }

    private RpcResponseEnvelope HandleCore(RpcRequestEnvelope request)
    {
        return request.Command switch
        {
            CommandNames.Status => RpcResponse.Success(
                request.Command,
                _runtime.GetStatus(),
                request.Id),

            CommandNames.CanvasSummary => RpcResponse.Success(
                request.Command,
                _runtime.GetCanvasSummary(DeserializeParams<CanvasSummaryRequest>(request.Params)),
                request.Id),

            CommandNames.NodeRead => RpcResponse.Success(
                request.Command,
                _runtime.NodeRead(DeserializeParams<NodeReadRequest>(request.Params)),
                request.Id),

            CommandNames.TxnApply => RpcResponse.Success(
                request.Command,
                _runtime.ApplyTransaction(DeserializeParams<TransactionApplyRequest>(request.Params)),
                request.Id),

            CommandNames.GraphApply => RpcResponse.Success(
                request.Command,
                _runtime.ApplyGraph(DeserializeParams<GraphApplyRequest>(request.Params)),
                request.Id),

            CommandNames.SolveRun => RpcResponse.Success(
                request.Command,
                _runtime.SolveRun(DeserializeParams<SolveRunRequest>(request.Params)),
                request.Id),

            CommandNames.DebugRead => RpcResponse.Success(
                request.Command,
                _runtime.DebugRead(DeserializeParams<DebugReadRequest>(request.Params)),
                request.Id),

            _ => RpcResponse.Failure(
                request.Command,
                "validation",
                $"Unsupported command '{request.Command}'.",
                request.Id)
        };
    }

    private static T ExecuteOnUiThread<T>(Func<T> action)
    {
        if (!RhinoApp.InvokeRequired)
        {
            return action();
        }

        T? result = default;
        Exception? failure = null;

        RhinoApp.InvokeAndWait(() =>
        {
            try
            {
                result = action();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        if (failure is not null)
        {
            throw failure;
        }

        return result!;
    }

    private static T DeserializeParams<T>(JsonElement? raw) where T : new()
    {
        if (raw is null || raw.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return new T();
        }

        return raw.Value.Deserialize<T>(ProtocolJson.Options) ?? new T();
    }
}
