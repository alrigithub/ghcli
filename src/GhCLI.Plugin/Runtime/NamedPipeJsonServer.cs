using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using GhCLI.Protocol;
using Rhino;

namespace GhCLI.Plugin.Runtime;

internal sealed class NamedPipeJsonServer : IDisposable
{
    private readonly string _pipeName;
    private readonly Func<RpcRequestEnvelope, CancellationToken, Task<RpcResponseEnvelope>> _handler;
    private readonly CancellationTokenSource _cts = new();
    private Task? _listenerTask;

    public NamedPipeJsonServer(
        string pipeName,
        Func<RpcRequestEnvelope, CancellationToken, Task<RpcResponseEnvelope>> handler)
    {
        _pipeName = pipeName;
        _handler = handler;
    }

    public void Start()
    {
        _listenerTask = Task.Run(() => ListenLoopAsync(_cts.Token), _cts.Token);
    }

    private async Task ListenLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var server = new NamedPipeServerStream(
                _pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            try
            {
                await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                _ = Task.Run(() => HandleClientAsync(server, cancellationToken), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                server.Dispose();
                return;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"[GhCLI] pipe accept failure: {ex.Message}");
                server.Dispose();
            }
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream stream, CancellationToken cancellationToken)
    {
        using var _ = stream;
        using var reader = new StreamReader(stream, Encoding.UTF8, false, 4096, leaveOpen: true);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, leaveOpen: true)
        {
            AutoFlush = true
        };

        while (!cancellationToken.IsCancellationRequested && stream.IsConnected)
        {
            string? line;
            try
            {
                line = await reader.ReadLineAsync().ConfigureAwait(false);
            }
            catch
            {
                return;
            }

            if (line is null)
            {
                return;
            }

            RpcResponseEnvelope response;
            try
            {
                var request = JsonSerializer.Deserialize<RpcRequestEnvelope>(line, ProtocolJson.Options);
                if (request is null || string.IsNullOrWhiteSpace(request.Command))
                {
                    response = RpcResponse.Failure("unknown", "validation", "Invalid RPC request envelope.");
                }
                else
                {
                    response = await _handler(request, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                response = RpcResponse.Failure("unknown", "runtime_failure", ex.Message);
            }

            var json = JsonSerializer.Serialize(response, ProtocolJson.Options);
            await writer.WriteLineAsync(json).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try
        {
            _listenerTask?.Wait(TimeSpan.FromSeconds(1));
        }
        catch
        {
            // no-op
        }

        _cts.Dispose();
    }
}
