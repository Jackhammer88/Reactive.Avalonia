namespace Reactive.Avalonia;

/// <summary>
/// The knobs available when calling <c>UseReactive()</c>.
/// </summary>
/// <remarks>
/// The defaults are already right for an Avalonia application. Change them for tests, or to run UI work at a
/// different dispatcher priority.
/// </remarks>
public sealed class ReactiveOptions
{
    /// <summary>
    /// Gets or sets the scheduler used to marshal work onto the UI thread.
    /// </summary>
    public IScheduler MainThreadScheduler { get; set; } = AvaloniaScheduler.Instance;

    /// <summary>
    /// Gets or sets the scheduler used for background work.
    /// </summary>
    public IScheduler TaskpoolScheduler { get; set; } = TaskPoolScheduler.Default;

    /// <summary>
    /// Gets or sets the observer that receives exceptions nobody else observed.
    /// </summary>
    /// <remarks>
    /// Leaving this <see langword="null"/> keeps the default, which rethrows on the UI thread. Supplying a
    /// handler means unobserved failures become log lines instead of crashes — deliberate, but make sure it is
    /// deliberate.
    /// </remarks>
    public IObserver<Exception>? DefaultExceptionHandler { get; set; }
}