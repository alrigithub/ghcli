using System.IO.Pipes;
using System.Text;

namespace GhCLI;

internal static class NamedPipeTransport
{
    public static async Task<string> SendAsync(
        string pipeName,
        string requestJson,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        using var client = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeoutMs);

        await client.ConnectAsync(timeoutCts.Token).ConfigureAwait(false);

        using var reader = new StreamReader(client, Encoding.UTF8, false, 4096, leaveOpen: true);
        using var writer = new StreamWriter(client, new UTF8Encoding(false), 4096, leaveOpen: true)
        {
            AutoFlush = true
        };

        await writer.WriteLineAsync(requestJson.AsMemory(), timeoutCts.Token).ConfigureAwait(false);
        var response = await reader.ReadLineAsync(timeoutCts.Token).ConfigureAwait(false);
        if (response is null)
        {
            throw new IOException("No response was received from the plugin pipe.");
        }

        return response;
    }
}
