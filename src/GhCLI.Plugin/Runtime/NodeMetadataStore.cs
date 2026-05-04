namespace GhCLI.Plugin.Runtime;

internal sealed class NodeMetadataStore
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, string> _guidToNodeId = new();
    private readonly Dictionary<string, Guid> _nodeIdToGuid = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PythonNodeMetadata> _pythonByNodeId = new(StringComparer.OrdinalIgnoreCase);

    public string GetOrCreateNodeId(Grasshopper.Kernel.IGH_DocumentObject obj, string fallbackPrefix)
    {
        lock (_gate)
        {
            if (_guidToNodeId.TryGetValue(obj.InstanceGuid, out var existing))
            {
                return existing;
            }

            var nickname = obj.NickName?.Trim();
            var candidate = !string.IsNullOrWhiteSpace(nickname) && nickname.StartsWith("AGENT_", StringComparison.OrdinalIgnoreCase)
                ? nickname
                : $"{fallbackPrefix}_{obj.InstanceGuid:N}".ToUpperInvariant();

            Bind(obj.InstanceGuid, candidate);
            return candidate;
        }
    }

    public void Bind(Guid guid, string nodeId)
    {
        lock (_gate)
        {
            _guidToNodeId[guid] = nodeId;
            _nodeIdToGuid[nodeId] = guid;
        }
    }

    public bool TryResolveGuid(string nodeId, out Guid guid)
    {
        lock (_gate)
        {
            return _nodeIdToGuid.TryGetValue(nodeId, out guid);
        }
    }

    public void SetPythonMetadata(string nodeId, PythonNodeMetadata metadata)
    {
        lock (_gate)
        {
            _pythonByNodeId[nodeId] = metadata;
        }
    }

    public bool TryGetPythonMetadata(string nodeId, out PythonNodeMetadata metadata)
    {
        lock (_gate)
        {
            return _pythonByNodeId.TryGetValue(nodeId, out metadata!);
        }
    }

    public void Remove(Guid guid, string nodeId)
    {
        lock (_gate)
        {
            _guidToNodeId.Remove(guid);
            _nodeIdToGuid.Remove(nodeId);
            _pythonByNodeId.Remove(nodeId);
        }
    }
}

internal sealed class PythonNodeMetadata
{
    public string Runtime { get; set; } = "cpython3";
    public string? FilePath { get; set; }
    public string? SourceHash { get; set; }
}
