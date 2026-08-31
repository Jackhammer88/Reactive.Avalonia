using System.Threading;

namespace Reactive.Avalonia;

/// <summary>
/// The schedulers this library uses by default.
/// </summary>
/// <remarks>
/// Both properties are already correct for an Avalonia application before <c>UseReactive()</c> runs; the builder
/// only exists so you can substitute them (for tests, or to change dispatcher priority).
/// </remarks>
public static class RxSchedulers
{
    private static IScheduler _mainThreadScheduler = AvaloniaScheduler.Instance;
    private static IScheduler _taskpoolScheduler = TaskPoolScheduler.Default;
    private static IObserver<Exception>? _defaultExceptionHandler;

    /// <summary>
    /// Gets or sets the scheduler that marshals work onto the UI thread. Defaults to <see cref="AvaloniaScheduler.Instance"/>.
    /// </summary>
    public static IScheduler MainThreadScheduler
    {
        get => Volatile.Read(ref _mainThreadScheduler);
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            Volatile.Write(ref _mainThreadScheduler, value);
        }
    }

    /// <summary>
    /// Gets or sets the scheduler used for background work. Defaults to <see cref="TaskPoolScheduler.Default"/>.
    /// </summary>
    public static IScheduler TaskpoolScheduler
    {
        get => Volatile.Read(ref _taskpoolScheduler);
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            Volatile.Write(ref _taskpoolScheduler, value);
        }
    }

    /// <summary>
    /// Gets or sets the observer that receives exceptions nobody else observed.
    /// </summary>
    /// <remarks>
    /// The default rethrows on <see cref="MainThreadScheduler"/>, turning a silently swallowed bug into a crash.
    /// Replace it to log instead — but be aware that you are then choosing to continue in an unknown state.
    /// </remarks>
    public static IObserver<Exception> DefaultExceptionHandler
    {
        get => Volatile.Read(ref _defaultExceptionHandler) ?? RethrowOnMainThread.Instance;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            Volatile.Write(ref _defaultExceptionHandler, value);
        }
    }

    /// <summary>
    /// Points the schedulers at the calling thread instead of the Avalonia dispatcher, for a process that has
    /// no UI.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call this once at the start of a console program, an integration harness or a background service that
    /// uses view models without ever showing them. Without it, anything scheduled onto
    /// <see cref="MainThreadScheduler"/> is posted to a dispatcher that nobody pumps, so awaiting a command
    /// hangs — sometimes, depending on which thread the value arrives on, which is worse than always.
    /// </para>
    /// <para>
    /// This is the whole of the setup. There is no builder to run and no services to register: the library has
    /// no service locator, so the schedulers are the only thing that depends on there being a UI.
    /// </para>
    /// <para>
    /// Do not call this from an Avalonia application — use <c>UseReactive()</c> on the
    /// <c>AppBuilder</c> instead, which is also what Avalonia's own headless test host needs, since that does
    /// have a dispatcher.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// public static async Task Main()
    /// {
    ///     RxSchedulers.UseCurrentThread();
    ///
    ///     var viewModel = new ImportViewModel();
    ///     await viewModel.BrowseFileCommand.Execute();
    /// }
    /// </code>
    /// </example>
    public static void UseCurrentThread()
    {
        MainThreadScheduler = CurrentThreadScheduler.Instance;
        TaskpoolScheduler = TaskPoolScheduler.Default;
    }

    /// <summary>
    /// Swaps the schedulers for the duration of a scope, then puts them back.
    /// </summary>
    /// <param name="mainThreadScheduler">The scheduler to use as <see cref="MainThreadScheduler"/>.</param>
    /// <param name="taskpoolScheduler">
    /// The scheduler to use as <see cref="TaskpoolScheduler"/>. Defaults to <paramref name="mainThreadScheduler"/>.
    /// </param>
    /// <returns>A token that restores the previous schedulers when disposed.</returns>
    /// <remarks>
    /// Written for tests: a <c>TestScheduler</c> or <see cref="ImmediateScheduler"/> makes reactive code
    /// deterministic. These are process-wide statics, so do not run scopes concurrently.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// using (RxSchedulers.With(ImmediateScheduler.Instance))
    /// {
    ///     // everything here resolves synchronously
    /// }
    /// </code>
    /// </example>
    public static IDisposable With(IScheduler mainThreadScheduler, IScheduler? taskpoolScheduler = null)
    {
        ArgumentNullException.ThrowIfNull(mainThreadScheduler);

        var previousMainThread = MainThreadScheduler;
        var previousTaskpool = TaskpoolScheduler;

        MainThreadScheduler = mainThreadScheduler;
        TaskpoolScheduler = taskpoolScheduler ?? mainThreadScheduler;

        return Disposable.Create(
            (previousMainThread, previousTaskpool),
            static state =>
            {
                MainThreadScheduler = state.previousMainThread;
                TaskpoolScheduler = state.previousTaskpool;
            });
    }

    private sealed class RethrowOnMainThread : IObserver<Exception>
    {
        public static readonly RethrowOnMainThread Instance = new();

        public void OnCompleted()
        {
        }

        public void OnError(Exception error) => Rethrow(error);

        public void OnNext(Exception value) => Rethrow(value);

        private static void Rethrow(Exception error) =>
            MainThreadScheduler.Schedule(error, static (_, e) => throw new UnhandledErrorException(e));
    }
}