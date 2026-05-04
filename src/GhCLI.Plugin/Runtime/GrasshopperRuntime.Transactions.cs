using System.Drawing;
using System.Reflection;
using System.Text.Json;
using GhCLI.Core.Errors;
using GhCLI.Core.Hashing;
using GhCLI.Core.Json;
using GhCLI.Protocol;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace GhCLI.Plugin.Runtime;

internal sealed partial class GrasshopperRuntime
{
    public TransactionApplyData ApplyTransaction(TransactionApplyRequest request)
    {
        lock (_gate)
        {
            if (request.Operations.Count == 0)
            {
                throw new CommandValidationException("txn.apply requires at least one operation.");
            }

            var doc = RequireActiveDocument();
            PreValidateTransaction(doc, request);

            var result = new TransactionApplyData();
            var wireOperations = new List<TransactionOperationModel>();

            foreach (var operation in request.Operations)
            {
                if (operation.Op == TransactionOps.SetWires)
                {
                    wireOperations.Add(operation);
                    continue;
                }

                ApplyTransactionOperation(doc, operation, result);
            }

            foreach (var operation in wireOperations)
            {
                ApplySetWires(doc, operation.Args, result);
            }

            if (request.SolveAfter || request.DebugAfter.Count > 0)
            {
                _ = SolveRun(new SolveRunRequest());
            }

            var summary = BuildCanvasSummary(doc, "full");
            result.Applied = true;
            result.GraphHash = summary.GraphHash;
            foreach (var nodeId in request.DebugAfter)
            {
                var target = ResolveNode(doc, nodeId, null)
                             ?? throw new CommandValidationException($"debugAfter node_id not found: {nodeId}");
                result.DebugReads.Add(BuildDebugRead(target));
            }

            return result;
        }
    }

    public TransactionApplyData ApplyGraph(GraphApplyRequest request)
    {
        var operations = new List<TransactionOperationModel>();
        operations.AddRange(request.Sliders.Select(x => CreateOperation(TransactionOps.UpsertSlider, x)));
        operations.AddRange(request.Toggles.Select(x => CreateOperation(TransactionOps.UpsertToggle, x)));
        operations.AddRange(request.Panels.Select(x => CreateOperation(TransactionOps.UpsertPanel, x)));
        operations.AddRange(request.Notes.Select(x => CreateOperation(TransactionOps.UpsertNote, x)));
        operations.AddRange(request.PythonNodes.Select(x => CreateOperation(TransactionOps.UpsertPythonNode, x)));

        if (request.Wires.Count > 0)
        {
            operations.Add(new TransactionOperationModel
            {
                Op = TransactionOps.SetWires,
                Args = ToJsonElement(new { connect = request.Wires })
            });
        }

        return ApplyTransaction(new TransactionApplyRequest
        {
            TransactionId = request.TransactionId,
            Operations = operations,
            SolveAfter = request.SolveAfter,
            DebugAfter = request.DebugAfter
        });
    }

    private static TransactionOperationModel CreateOperation(string op, JsonElement args)
    {
        return new TransactionOperationModel
        {
            Op = op,
            Args = args.Clone()
        };
    }

    private static JsonElement ToJsonElement<T>(T value)
    {
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(value, ProtocolJson.Options));
        return doc.RootElement.Clone();
    }

    private void ApplyTransactionOperation(GH_Document doc, TransactionOperationModel operation, TransactionApplyData result)
    {
        switch (operation.Op)
        {
            case TransactionOps.UpsertPythonNode:
                ApplyUpsertPythonNode(doc, operation.Args, result);
                break;
            case TransactionOps.UpsertSlider:
                ApplyUpsertSlider(doc, operation.Args, result);
                break;
            case TransactionOps.UpsertToggle:
                ApplyUpsertToggle(doc, operation.Args, result);
                break;
            case TransactionOps.UpsertPanel:
                ApplyUpsertPanel(doc, operation.Args, result);
                break;
            case TransactionOps.UpsertNote:
                ApplyUpsertNote(doc, operation.Args, result);
                break;
            case TransactionOps.MoveNode:
                ApplyMoveNode(doc, operation.Args, result);
                break;
            case TransactionOps.SetValue:
                ApplySetValue(doc, operation.Args, result);
                break;
            default:
                throw new CommandValidationException($"Unsupported transaction op '{operation.Op}'.");
        }
    }

    private void PreValidateTransaction(GH_Document doc, TransactionApplyRequest request)
    {
        var knownNodeIds = doc.Objects
            .OfType<IGH_DocumentObject>()
            .Select(obj => _nodeMetadata.GetOrCreateNodeId(obj, KindPrefix(DetermineKind(obj))))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var operation in request.Operations)
        {
            if (operation.Args.ValueKind != JsonValueKind.Object)
            {
                throw new CommandValidationException($"Operation '{operation.Op}' requires args object.");
            }

            switch (operation.Op)
            {
                case TransactionOps.UpsertPythonNode:
                case TransactionOps.UpsertSlider:
                case TransactionOps.UpsertToggle:
                case TransactionOps.UpsertPanel:
                case TransactionOps.UpsertNote:
                    {
                        var nodeId = operation.Args.RequireString("node_id");
                        knownNodeIds.Add(nodeId);
                        if (operation.Op == TransactionOps.UpsertPythonNode)
                        {
                            var filePath = operation.Args.RequireString("file_path");
                            var resolved = _fileResolver.ResolvePath(filePath);
                            if (!File.Exists(resolved))
                            {
                                throw new CommandValidationException($"Python source file not found: {resolved}");
                            }
                        }

                        break;
                    }
                case TransactionOps.MoveNode:
                case TransactionOps.SetValue:
                    {
                        var nodeId = operation.Args.RequireString("node_id");
                        if (!knownNodeIds.Contains(nodeId))
                        {
                            throw new CommandValidationException($"Unknown node_id '{nodeId}' for op '{operation.Op}'.");
                        }

                        break;
                    }
                case TransactionOps.SetWires:
                    {
                        foreach (var wire in operation.Args.GetArray("connect"))
                        {
                            ValidateWireEndpoints(knownNodeIds, wire, operation.Op);
                        }

                        foreach (var wire in operation.Args.GetArray("disconnect"))
                        {
                            ValidateWireEndpoints(knownNodeIds, wire, operation.Op);
                        }

                        break;
                    }
                default:
                    throw new CommandValidationException($"Unsupported transaction op '{operation.Op}'.");
            }
        }

        foreach (var nodeId in request.DebugAfter)
        {
            if (!knownNodeIds.Contains(nodeId))
            {
                throw new CommandValidationException($"Unknown node_id '{nodeId}' in debugAfter.");
            }
        }
    }

    private static void ValidateWireEndpoints(HashSet<string> knownNodeIds, JsonElement wire, string opName)
    {
        if (wire.ValueKind != JsonValueKind.Object)
        {
            throw new CommandValidationException($"Wire entry in '{opName}' must be an object.");
        }

        var source = wire.RequireString("source_node_id");
        var target = wire.RequireString("target_node_id");
        if (!knownNodeIds.Contains(source))
        {
            throw new CommandValidationException($"Unknown source_node_id '{source}' in {opName}.");
        }

        if (!knownNodeIds.Contains(target))
        {
            throw new CommandValidationException($"Unknown target_node_id '{target}' in {opName}.");
        }
    }

    private void ApplyUpsertPythonNode(GH_Document doc, JsonElement args, TransactionApplyData result)
    {
        var nodeId = args.RequireString("node_id");
        var runtime = args.GetOptionalString("runtime") ?? "cpython3";
        var filePath = args.RequireString("file_path");
        var resolvedPath = _fileResolver.ResolvePath(filePath);
        var source = File.ReadAllText(resolvedPath);
        var nickname = args.GetOptionalString("nickname") ?? nodeId;
        var position = ReadPosition(args, 160, 120);

        var node = ResolveNode(doc, nodeId, null);
        if (node is null)
        {
            node = CreatePythonNode(runtime);
            if (node is null)
            {
                throw new CommandValidationException("Could not create a Rhino 8 Python script component.");
            }

            node.CreateAttributes();
            doc.AddObject(node, false);
            _nodeMetadata.Bind(node.InstanceGuid, nodeId);
            result.Created.Add(nodeId);
        }
        else
        {
            if (!IsPythonNode(node))
            {
                throw new CommandValidationException($"node_id '{nodeId}' exists but is not a Python node.");
            }

            result.Patched.Add(nodeId);
        }

        node.NickName = nickname;
        SetPosition(node, position);

        if (!TrySetPythonSource(node, source))
        {
            result.Warnings.Add($"Could not set Python source for '{nodeId}' via known script APIs.");
        }

        ApplyPortSchema(node, GetOptionalArray(args, "inputs"), GetOptionalArray(args, "outputs"), result.Warnings);

        _nodeMetadata.SetPythonMetadata(nodeId, new PythonNodeMetadata
        {
            Runtime = runtime,
            FilePath = resolvedPath,
            SourceHash = Sha256Hasher.ShortHash(source)
        });

        if (node is IGH_ActiveObject active)
        {
            active.ExpireSolution(false);
        }
    }

    private void ApplyUpsertSlider(GH_Document doc, JsonElement args, TransactionApplyData result)
    {
        var nodeId = args.RequireString("node_id");
        var nickname = args.GetOptionalString("nickname") ?? nodeId;
        var position = ReadPosition(args, 80, 80);
        var min = args.GetOptionalDouble("min") ?? 0d;
        var max = args.GetOptionalDouble("max") ?? 1d;
        var value = args.GetOptionalDouble("value") ?? min;

        var existing = ResolveNode(doc, nodeId, null);
        GH_NumberSlider slider;

        if (existing is null)
        {
            slider = new GH_NumberSlider();
            slider.CreateAttributes();
            doc.AddObject(slider, false);
            _nodeMetadata.Bind(slider.InstanceGuid, nodeId);
            result.Created.Add(nodeId);
        }
        else if (existing is GH_NumberSlider existingSlider)
        {
            slider = existingSlider;
            result.Patched.Add(nodeId);
        }
        else
        {
            throw new CommandValidationException($"node_id '{nodeId}' exists but is not a slider.");
        }

        slider.NickName = nickname;
        SetPosition(slider, position);
        SetSliderRangeAndValue(slider, min, max, value);
    }

    private void ApplyUpsertToggle(GH_Document doc, JsonElement args, TransactionApplyData result)
    {
        var nodeId = args.RequireString("node_id");
        var nickname = args.GetOptionalString("nickname") ?? nodeId;
        var position = ReadPosition(args, 80, 120);
        var value = args.GetOptionalBool("value");

        var existing = ResolveNode(doc, nodeId, null);
        GH_BooleanToggle toggle;

        if (existing is null)
        {
            toggle = new GH_BooleanToggle();
            toggle.CreateAttributes();
            doc.AddObject(toggle, false);
            _nodeMetadata.Bind(toggle.InstanceGuid, nodeId);
            result.Created.Add(nodeId);
        }
        else if (existing is GH_BooleanToggle existingToggle)
        {
            toggle = existingToggle;
            result.Patched.Add(nodeId);
        }
        else
        {
            throw new CommandValidationException($"node_id '{nodeId}' exists but is not a toggle.");
        }

        toggle.NickName = nickname;
        SetPosition(toggle, position);
        TrySetPropertyValue(toggle, "Value", value);
    }

    private void ApplyUpsertPanel(GH_Document doc, JsonElement args, TransactionApplyData result)
    {
        var nodeId = args.RequireString("node_id");
        var nickname = args.GetOptionalString("nickname") ?? nodeId;
        var position = ReadPosition(args, 80, 160);
        var text = args.GetOptionalString("text") ?? string.Empty;

        var existing = ResolveNode(doc, nodeId, null);
        GH_Panel panel;

        if (existing is null)
        {
            panel = new GH_Panel();
            panel.CreateAttributes();
            doc.AddObject(panel, false);
            _nodeMetadata.Bind(panel.InstanceGuid, nodeId);
            result.Created.Add(nodeId);
        }
        else if (existing is GH_Panel existingPanel)
        {
            panel = existingPanel;
            result.Patched.Add(nodeId);
        }
        else
        {
            throw new CommandValidationException($"node_id '{nodeId}' exists but is not a panel.");
        }

        panel.NickName = nickname;
        SetPosition(panel, position);
        if (!TrySetPropertyValue(panel, "UserText", text))
        {
            _ = TrySetPropertyValue(panel, "Text", text);
        }
    }

    private void ApplyUpsertNote(GH_Document doc, JsonElement args, TransactionApplyData result)
    {
        var nodeId = args.RequireString("node_id");
        var nickname = args.GetOptionalString("nickname") ?? nodeId;
        var position = ReadPosition(args, 80, 220);
        var text = args.GetOptionalString("text") ?? nickname;

        var existing = ResolveNode(doc, nodeId, null);
        GH_Scribble note;

        if (existing is null)
        {
            note = new GH_Scribble();
            note.CreateAttributes();
            doc.AddObject(note, false);
            _nodeMetadata.Bind(note.InstanceGuid, nodeId);
            result.Created.Add(nodeId);
        }
        else if (existing is GH_Scribble existingNote)
        {
            note = existingNote;
            result.Patched.Add(nodeId);
        }
        else
        {
            throw new CommandValidationException($"node_id '{nodeId}' exists but is not a note.");
        }

        note.NickName = nickname;
        SetPosition(note, position);
        _ = TrySetPropertyValue(note, "Text", text);
    }

    private void ApplySetWires(GH_Document doc, JsonElement args, TransactionApplyData result)
    {
        foreach (var entry in args.GetArray("disconnect"))
        {
            ApplyWireOperation(doc, entry, result, connect: false);
        }

        foreach (var entry in args.GetArray("connect"))
        {
            ApplyWireOperation(doc, entry, result, connect: true);
        }
    }

    private void ApplyWireOperation(GH_Document doc, JsonElement entry, TransactionApplyData result, bool connect)
    {
        var sourceId = entry.RequireString("source_node_id");
        var sourcePort = entry.GetOptionalString("source_port") ?? "0";
        var targetId = entry.RequireString("target_node_id");
        var targetPort = entry.GetOptionalString("target_port") ?? "0";

        var sourceNode = ResolveNode(doc, sourceId, null)
                         ?? throw new CommandValidationException($"Wire source node not found: {sourceId}");
        var targetNode = ResolveNode(doc, targetId, null)
                         ?? throw new CommandValidationException($"Wire target node not found: {targetId}");

        var sourceParam = ResolvePort(sourceNode, isOutput: true, sourcePort)
                          ?? throw new CommandValidationException($"Wire source port not found: {sourceId}.{sourcePort}");
        var targetParam = ResolvePort(targetNode, isOutput: false, targetPort)
                          ?? throw new CommandValidationException($"Wire target port not found: {targetId}.{targetPort}");

        if (connect)
        {
            targetParam.AddSource(sourceParam);
        }
        else
        {
            targetParam.RemoveSource(sourceParam);
        }

        result.WireChanges++;
    }

    private void ApplyMoveNode(GH_Document doc, JsonElement args, TransactionApplyData result)
    {
        var nodeId = args.RequireString("node_id");
        var node = ResolveNode(doc, nodeId, null)
                   ?? throw new CommandValidationException($"Unknown node_id '{nodeId}' for move_node.");

        var position = ReadPosition(args, node.Attributes?.Pivot.X ?? 0, node.Attributes?.Pivot.Y ?? 0);
        SetPosition(node, position);
        result.Patched.Add(nodeId);
    }

    private void ApplySetValue(GH_Document doc, JsonElement args, TransactionApplyData result)
    {
        var nodeId = args.RequireString("node_id");
        var node = ResolveNode(doc, nodeId, null)
                   ?? throw new CommandValidationException($"Unknown node_id '{nodeId}' for set_value.");

        if (!args.TryGetPropertyIgnoreCase("value", out var valueElement))
        {
            throw new CommandValidationException("set_value requires a value field.");
        }

        if (node is GH_NumberSlider slider)
        {
            var value = valueElement.ValueKind == JsonValueKind.Number
                ? valueElement.GetDouble()
                : double.Parse(valueElement.GetString() ?? "0");
            SetSliderValue(slider, value);
        }
        else if (node is GH_BooleanToggle toggle)
        {
            var value = valueElement.ValueKind == JsonValueKind.True ||
                        (valueElement.ValueKind == JsonValueKind.String &&
                         bool.Parse(valueElement.GetString() ?? "false"));
            _ = TrySetPropertyValue(toggle, "Value", value);
        }
        else if (node is GH_Panel panel)
        {
            var text = valueElement.ValueKind == JsonValueKind.String
                ? valueElement.GetString() ?? string.Empty
                : valueElement.ToString();
            if (!TrySetPropertyValue(panel, "UserText", text))
            {
                _ = TrySetPropertyValue(panel, "Text", text);
            }
        }
        else
        {
            throw new CommandValidationException(
                $"set_value does not support node '{nodeId}' of type {node.GetType().Name}.");
        }

        if (node is IGH_ActiveObject active)
        {
            active.ExpireSolution(false);
        }

        result.Patched.Add(nodeId);
    }

    private static IReadOnlyList<JsonElement>? GetOptionalArray(JsonElement element, string name)
    {
        if (!element.TryGetPropertyIgnoreCase(name, out var value))
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new CommandValidationException($"'{name}' must be an array when provided.");
        }

        return value.EnumerateArray().Select(x => x.Clone()).ToArray();
    }

    private static PositionModel ReadPosition(JsonElement args, double defaultX, double defaultY)
    {
        if (args.TryGetPropertyIgnoreCase("position", out var position) && position.ValueKind == JsonValueKind.Object)
        {
            return new PositionModel
            {
                X = position.GetOptionalDouble("x") ?? defaultX,
                Y = position.GetOptionalDouble("y") ?? defaultY
            };
        }

        return new PositionModel { X = defaultX, Y = defaultY };
    }

    private static void SetPosition(IGH_DocumentObject node, PositionModel position)
    {
        node.CreateAttributes();
        if (node.Attributes is null)
        {
            return;
        }

        node.Attributes.Pivot = new PointF((float)position.X, (float)position.Y);
        node.Attributes.ExpireLayout();
    }

    private IGH_DocumentObject? CreatePythonNode(string runtime)
    {
        var runtimeLower = runtime.ToLowerInvariant();
        var proxies = Instances.ComponentServer?.ObjectProxies;
        if (proxies is null)
        {
            return null;
        }

        Guid? selectedGuid = null;
        Guid? fallbackGuid = null;

        foreach (var proxy in proxies)
        {
            var name = proxy.Desc?.Name ?? string.Empty;
            if (!name.Contains("Python", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            fallbackGuid ??= proxy.Guid;
            if (runtimeLower.Contains("cpython", StringComparison.OrdinalIgnoreCase) ||
                runtimeLower.Contains("python3", StringComparison.OrdinalIgnoreCase))
            {
                if (name.Contains("Python 3", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("CPython", StringComparison.OrdinalIgnoreCase))
                {
                    selectedGuid = proxy.Guid;
                    break;
                }
            }
            else if (name.Contains("IronPython", StringComparison.OrdinalIgnoreCase))
            {
                selectedGuid = proxy.Guid;
                break;
            }
        }

        var guid = selectedGuid ?? fallbackGuid;
        if (guid is null)
        {
            return null;
        }

        return Instances.ComponentServer?.EmitObject(guid.Value) as IGH_DocumentObject;
    }

    private static bool TrySetPythonSource(IGH_DocumentObject node, string source)
    {
        string[] propertyNames = { "Code", "SourceCode", "Script", "ScriptSource", "Source" };
        foreach (var property in propertyNames)
        {
            if (TrySetPropertyValue(node, property, source))
            {
                return true;
            }
        }

        string[] methodNames = { "SetCode", "SetSource", "SetScript", "SetScriptSource" };
        foreach (var methodName in methodNames)
        {
            var methods = node.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.Name == methodName)
                .ToArray();

            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string))
                {
                    continue;
                }

                method.Invoke(node, new object[] { source });
                return true;
            }
        }

        return false;
    }

    private static string? TryGetPythonSource(IGH_DocumentObject node)
    {
        string[] propertyNames = { "Code", "SourceCode", "Script", "ScriptSource", "Source" };
        foreach (var property in propertyNames)
        {
            if (TryGetPropertyValue(node, property) is string text && !string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return null;
    }

    private static void SetSliderRangeAndValue(GH_NumberSlider slider, double min, double max, double value)
    {
        var sliderCore = TryGetPropertyValue(slider, "Slider");
        if (sliderCore is not null)
        {
            _ = TrySetPropertyValue(sliderCore, "Minimum", ConvertToType(min, TryGetPropertyType(sliderCore, "Minimum")));
            _ = TrySetPropertyValue(sliderCore, "Maximum", ConvertToType(max, TryGetPropertyType(sliderCore, "Maximum")));
        }

        SetSliderValue(slider, value);
    }

    private static void SetSliderValue(GH_NumberSlider slider, double value)
    {
        if (TryInvokeNumeric(slider, "SetSliderValue", value))
        {
            return;
        }

        var sliderCore = TryGetPropertyValue(slider, "Slider");
        if (sliderCore is not null)
        {
            _ = TrySetPropertyValue(sliderCore, "Value", ConvertToType(value, TryGetPropertyType(sliderCore, "Value")));
        }
    }
}
