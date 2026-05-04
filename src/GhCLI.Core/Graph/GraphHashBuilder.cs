using System.Text.Json;
using GhCLI.Core.Hashing;
using GhCLI.Protocol;

namespace GhCLI.Core.Graph;

public static class GraphHashBuilder
{
    public static string Build(
        IReadOnlyCollection<CanvasNodeSummaryModel> nodes,
        IReadOnlyCollection<CanvasEdgeSummaryModel> edges)
    {
        var payload = new
        {
            nodes = nodes
                .OrderBy(n => n.NodeId, StringComparer.Ordinal)
                .Select(n => new
                {
                    n.NodeId,
                    n.Kind,
                    n.Nickname,
                    x = Math.Round(n.Position.X, 2),
                    y = Math.Round(n.Position.Y, 2),
                    inputs = n.Inputs.Select(p => p.Name).OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                    outputs = n.Outputs.Select(p => p.Name).OrderBy(x => x, StringComparer.Ordinal).ToArray()
                })
                .ToArray(),
            edges = edges
                .OrderBy(e => e.SourceNodeId, StringComparer.Ordinal)
                .ThenBy(e => e.SourcePort, StringComparer.Ordinal)
                .ThenBy(e => e.TargetNodeId, StringComparer.Ordinal)
                .ThenBy(e => e.TargetPort, StringComparer.Ordinal)
                .Select(e => new { e.SourceNodeId, e.SourcePort, e.TargetNodeId, e.TargetPort })
                .ToArray()
        };

        var json = JsonSerializer.Serialize(payload, ProtocolJson.Options);
        return Sha256Hasher.ShortHash(json);
    }
}
