namespace DialogueDown.Common.Errors;

/// <summary>
/// Base for every domain fault DialogueDown raises. Catch this to handle any
/// DialogueDown error broadly, or a derived type to handle one narrowly.
/// </summary>
internal abstract class DialogueDownException : Exception
{
    protected DialogueDownException(string message)
        : base(message)
    {
    }

    protected DialogueDownException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
