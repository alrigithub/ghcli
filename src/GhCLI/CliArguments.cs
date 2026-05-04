namespace GhCLI;

internal sealed class CliArguments
{
    public required string Command { get; init; }
    public Dictionary<string, string?> Options { get; } = new(StringComparer.OrdinalIgnoreCase);

    public static CliArguments Parse(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException("A command is required.");
        }

        var parsed = new CliArguments { Command = args[0].Trim() };
        for (var i = 1; i < args.Length; i++)
        {
            var token = args[i];
            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var key = token[2..];
            string? value = null;

            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                value = args[i + 1];
                i++;
            }
            else
            {
                value = "true";
            }

            parsed.Options[key] = value;
        }

        return parsed;
    }
}
