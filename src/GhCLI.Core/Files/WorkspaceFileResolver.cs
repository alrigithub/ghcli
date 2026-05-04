namespace GhCLI.Core.Files;

public sealed class WorkspaceFileResolver
{
    private readonly string _workspaceRoot;

    public WorkspaceFileResolver(string? workspaceRoot = null)
    {
        _workspaceRoot = string.IsNullOrWhiteSpace(workspaceRoot)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(workspaceRoot);
    }

    public string WorkspaceRoot => _workspaceRoot;

    public string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("file_path is required.", nameof(path));
        }

        var resolved = Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(_workspaceRoot, path));

        return resolved;
    }

    public string ReadAllText(string path)
    {
        var resolved = ResolvePath(path);
        if (!File.Exists(resolved))
        {
            throw new FileNotFoundException($"Python source file not found: {resolved}", resolved);
        }

        return File.ReadAllText(resolved);
    }
}
