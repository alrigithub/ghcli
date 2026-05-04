using System.Diagnostics;
using GhCLI.Core.Errors;
using GhCLI.Core.Files;
using GhCLI.Protocol;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino;

namespace GhCLI.Plugin.Runtime;

internal sealed partial class GrasshopperRuntime
{
    private readonly object _gate = new();
    private readonly string _sessionId;
    private readonly NodeMetadataStore _nodeMetadata = new();
    private readonly WorkspaceFileResolver _fileResolver = new();
    private double _lastSolveMs;

    public GrasshopperRuntime(string sessionId)
    {
        _sessionId = sessionId;
    }

    public StatusData GetStatus()
    {
        lock (_gate)
        {
            var doc = TryGetActiveDocument();
            return new StatusData
            {
                PluginLoaded = true,
                PipeConnected = true,
                RhinoVersion = RhinoApp.Version.ToString(),
                GrasshopperLoaded = Instances.ActiveCanvas is not null || doc is not null,
                ActiveDocument = doc is null
                    ? null
                    : new ActiveDocumentModel
                    {
                        Id = TryGetPropertyValue(doc, "DocumentID")?.ToString(),
                        Name = doc.DisplayName,
                        FilePath = TryGetPropertyValue(doc, "FilePath")?.ToString()
                    },
                Capabilities = new List<string>
                {
                    CommandNames.Status,
                    CommandNames.CanvasSummary,
                    CommandNames.NodeRead,
                    CommandNames.TxnApply,
                    CommandNames.GraphApply,
                    CommandNames.SolveRun,
                    CommandNames.DebugRead,
                    TransactionOps.UpsertPythonNode,
                    TransactionOps.UpsertSlider,
                    TransactionOps.UpsertToggle,
                    TransactionOps.UpsertPanel,
                    TransactionOps.UpsertNote,
                    TransactionOps.SetWires,
                    TransactionOps.MoveNode,
                    TransactionOps.SetValue
                }
            };
        }
    }

    public CanvasSummaryData GetCanvasSummary(CanvasSummaryRequest request)
    {
        lock (_gate)
        {
            var doc = RequireActiveDocument();
            return BuildCanvasSummary(doc, request.Scope);
        }
    }

    public NodeReadData NodeRead(NodeReadRequest request)
    {
        lock (_gate)
        {
            var doc = RequireActiveDocument();
            return BuildNodeRead(doc, request);
        }
    }

    public SolveRunData SolveRun(SolveRunRequest request)
    {
        lock (_gate)
        {
            var doc = RequireActiveDocument();
            if (!string.IsNullOrWhiteSpace(request.NodeId))
            {
                var target = ResolveNode(doc, request.NodeId, null);
                if (target is IGH_ActiveObject active)
                {
                    active.ExpireSolution(true);
                }
            }

            var sw = Stopwatch.StartNew();
            doc.NewSolution(false);
            sw.Stop();
            _lastSolveMs = sw.Elapsed.TotalMilliseconds;

            var runtimeMessages = string.IsNullOrWhiteSpace(request.NodeId)
                ? CollectRuntimeMessages(doc.Objects.OfType<IGH_DocumentObject>())
                : CollectRuntimeMessages(ResolveNode(doc, request.NodeId, null)).ToList();

            return new SolveRunData
            {
                Success = true,
                SolveMs = _lastSolveMs,
                RuntimeMessages = runtimeMessages.Take(100).ToList()
            };
        }
    }

    public DebugReadData DebugRead(DebugReadRequest request)
    {
        lock (_gate)
        {
            var doc = RequireActiveDocument();
            var target = ResolveNode(doc, request.NodeId, null);
            if (target is null)
            {
                throw new CommandValidationException("debug.read requires a valid node_id.");
            }

            return BuildDebugRead(target);
        }
    }

    private GH_Document RequireActiveDocument()
    {
        return TryGetActiveDocument()
               ?? throw new PluginUnavailableException("No active Grasshopper document is available.");
    }

    private static GH_Document? TryGetActiveDocument()
    {
        if (Instances.ActiveCanvas?.Document is GH_Document active)
        {
            return active;
        }

        var server = Instances.DocumentServer;
        if (server is not null)
        {
            foreach (var doc in server)
            {
                if (doc is GH_Document typed)
                {
                    return typed;
                }
            }
        }

        return null;
    }
}
