namespace Reactive.Avalonia;

/// <summary>
/// Thrown when an interaction reaches the end of its handler chain without being answered.
/// </summary>
/// <remarks>
/// In practice this means the view that was supposed to register a handler is not on screen — usually because
/// the handler was registered outside a <c>WhenActivated</c> block, or the view model outlived its view.
/// </remarks>
public sealed class UnhandledInteractionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnhandledInteractionException"/> class.
    /// </summary>
    public UnhandledInteractionException()
        : base("No handler answered the interaction.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnhandledInteractionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public UnhandledInteractionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnhandledInteractionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying error.</param>
    public UnhandledInteractionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}