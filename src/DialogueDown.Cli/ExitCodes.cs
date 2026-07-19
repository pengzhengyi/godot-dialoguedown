namespace DialogueDown.Cli;

/// <summary>Process exit codes the CLI returns, following the sysexits convention.</summary>
internal static class ExitCodes
{
    /// <summary>The command succeeded.</summary>
    public const int Success = 0;

    /// <summary>An unexpected failure.</summary>
    public const int Error = 1;

    /// <summary>The input data was incorrect (EX_DATAERR): a script or config error.</summary>
    public const int DataError = 65;

    /// <summary>A bad argument or usage (EX_USAGE).</summary>
    public const int UsageError = 64;

    /// <summary>A requested capability is not built yet (EX_SOFTWARE).</summary>
    public const int NotImplemented = 70;
}
