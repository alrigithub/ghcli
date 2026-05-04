namespace GhCLI.Core.Errors;

public class CommandValidationException : Exception
{
    public CommandValidationException(string message) : base(message)
    {
    }
}

public class PluginUnavailableException : Exception
{
    public PluginUnavailableException(string message) : base(message)
    {
    }
}
