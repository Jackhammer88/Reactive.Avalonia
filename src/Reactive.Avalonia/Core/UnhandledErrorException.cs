namespace Reactive.Avalonia;

/// <summary>
/// Wraps an exception that reached <see cref="RxSchedulers.DefaultExceptionHandler"/> because nothing was
/// subscribed to the <see cref="IHandleObservableErrors.ThrownExceptions"/> sequence that produced it.
/// </summary>
public sealed class UnhandledErrorException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnhandledErrorException"/> class.
    /// </summary>
    public UnhandledErrorException()
        : base("An observable pipeline failed and nothing was subscribed to its ThrownExceptions.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnhandledErrorException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public UnhandledErrorException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnhandledErrorException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that was not observed.</param>
    public UnhandledErrorException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnhandledErrorException"/> class.
    /// </summary>
    /// <param name="innerException">The exception that was not observed.</param>
    public UnhandledErrorException(Exception innerException)
        : base("An observable pipeline failed and nothing was subscribed to its ThrownExceptions.", innerException)
    {
    }
}