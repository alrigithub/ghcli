using Rhino;

namespace GhCLI.Plugin.Runtime;

internal static class PluginHost
{
    private static readonly object Gate = new();
    private static bool _started;
    private static NamedPipeJsonServer? _server;
    private static CommandRouter? _router;
    private static GrasshopperRuntime? _runtime;

    public static string SessionId { get; } = Guid.NewGuid().ToString("N");
    public static string PipeName =>
        Environment.GetEnvironmentVariable("GHCLI_PIPE_NAME")?.Trim() is { Length: > 0 } envName
            ? envName
            : "ghcli.v1";

    public static void Start()
    {
        lock (Gate)
        {
            if (_started)
            {
                return;
            }

            _runtime = new GrasshopperRuntime(SessionId);
            _router = new CommandRouter(_runtime);
            _server = new NamedPipeJsonServer(PipeName, _router.HandleAsync);
            _server.Start();
            _started = true;

            AppDomain.CurrentDomain.ProcessExit += (_, _) => Stop();
            AppDomain.CurrentDomain.DomainUnload += (_, _) => Stop();
            RhinoApp.WriteLine($"[GhCLI] pipe server started on '{PipeName}'.");
        }
    }

    public static void Stop()
    {
        lock (Gate)
        {
            if (!_started)
            {
                return;
            }

            try
            {
                _server?.Dispose();
            }
            catch
            {
                // no-op
            }

            _server = null;
            _router = null;
            _runtime = null;
            _started = false;
        }
    }
}
