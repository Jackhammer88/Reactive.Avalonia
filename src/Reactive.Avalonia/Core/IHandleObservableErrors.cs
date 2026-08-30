namespace Reactive.Avalonia;

/// <summary>
/// Implemented by objects that surface out-of-band errors raised by their internal observable pipelines.
/// </summary>
public interface IHandleObservableErrors
{
    /// <summary>
    /// Gets a sequence of exceptions that were thrown by the object's internal pipelines.
    /// </summary>
    /// <remarks>
    /// If nothing is subscribed to this sequence, exceptions are rethrown on
    /// <see cref="RxSchedulers.MainThreadScheduler"/> and will crash the application, which is deliberate:
    /// an unobserved error is a bug, not a warning.
    /// </remarks>
    IObservable<Exception> ThrownExceptions { get; }
}