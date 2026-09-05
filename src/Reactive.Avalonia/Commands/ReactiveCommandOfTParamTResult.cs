using System.Threading;
using System.Windows.Input;

namespace Reactive.Avalonia;

/// <summary>
/// A command whose execution is an observable sequence.
/// </summary>
/// <typeparam name="TParam">The parameter type. Use <see cref="Unit"/> when the command takes nothing.</typeparam>
/// <typeparam name="TResult">The result type. Use <see cref="Unit"/> when the command returns nothing.</typeparam>
/// <remarks>
/// <para>
/// Create instances through the <see cref="ReactiveCommand"/> factory methods rather than this constructor.
/// </para>
/// <para>
/// <see cref="Execute(TParam)"/> returns a cold sequence: the work starts when you subscribe, and disposing
/// that subscription cancels it — which is what makes the <see cref="CancellationToken"/> handed to
/// <see cref="ReactiveCommand.CreateFromTask(Func{CancellationToken, Task}, IObservable{bool}?, IScheduler?)"/>
/// mean anything. Binding a control to the command goes through <see cref="ICommand"/>, which subscribes for
/// you.
/// </para>
/// </remarks>
public sealed class ReactiveCommand<TParam, TResult> : IReactiveCommand, IObservable<TResult>
{
    private readonly Func<TParam, IObservable<TResult>> _execute;
    private readonly IScheduler _outputScheduler;
    private readonly BehaviorSubject<bool> _isExecuting = new(false);
    private readonly Subject<TResult> _results = new();
    private readonly ScheduledSubject<Exception> _exceptions;
    private readonly IDisposable _canExecuteSubscription;
    private volatile bool _canExecuteLatest;
    private int _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactiveCommand{TParam, TResult}"/> class.
    /// </summary>
    /// <param name="execute">Produces the sequence for one execution.</param>
    /// <param name="canExecute">Gates execution. Defaults to always executable.</param>
    /// <param name="outputScheduler">
    /// The scheduler results and state changes are delivered on. Defaults to
    /// <see cref="RxSchedulers.MainThreadScheduler"/>.
    /// </param>
    internal ReactiveCommand(
        Func<TParam, IObservable<TResult>> execute,
        IObservable<bool>? canExecute,
        IScheduler? outputScheduler)
    {
        _execute = execute;
        _outputScheduler = outputScheduler ?? RxSchedulers.MainThreadScheduler;
        _exceptions = new ScheduledSubject<Exception>(_outputScheduler, RxSchedulers.DefaultExceptionHandler);

        CanExecute = (canExecute ?? Observable.Return(true))
            .Catch<bool, Exception>(ex =>
            {
                _exceptions.OnNext(ex);
                return Observable.Return(false);
            })
            .StartWith(false)
            .CombineLatest(_isExecuting, static (allowed, executing) => allowed && !executing)
            .DistinctUntilChanged()
            .ObserveOn(_outputScheduler)
            .Replay(1)
            .RefCount();

        IsExecuting = _isExecuting.DistinctUntilChanged().ObserveOn(_outputScheduler);

        _canExecuteSubscription = CanExecute.Subscribe(allowed =>
        {
            _canExecuteLatest = allowed;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    /// <inheritdoc/>
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc/>
    public IObservable<bool> CanExecute { get; }

    /// <inheritdoc/>
    public IObservable<bool> IsExecuting { get; }

    /// <inheritdoc/>
    public IObservable<Exception> ThrownExceptions => _exceptions;

    /// <summary>
    /// Runs the command.
    /// </summary>
    /// <param name="parameter">The parameter to pass to the command body.</param>
    /// <returns>
    /// A cold sequence carrying the result. Nothing happens until you subscribe, and disposing the
    /// subscription cancels the execution.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Failures reach <see cref="ThrownExceptions"/> as well as the returned sequence, so a caller that only
    /// cares about the happy path can leave error handling to whoever is watching the command.
    /// </para>
    /// <para>
    /// Execution is not blocked by <see cref="CanExecute"/> — that gate is for the UI. Check it yourself if you
    /// are invoking the command from code and care.
    /// </para>
    /// </remarks>
    public IObservable<TResult> Execute(TParam parameter = default!)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

        try
        {
            var published = Observable
                .Defer(() =>
                {
                    _isExecuting.OnNext(true);
                    return _execute(parameter);
                })
                .Finally(() => _isExecuting.OnNext(false))
                .Publish();

            // Bookkeeping subscribes to the published subject directly, which keeps it out of the reference
            // count below — and, unlike a subscriber layered on top, lets an unobserved failure escape rather
            // than being converted into yet another OnError that the caller can swallow.
            published.Subscribe(
                result => _results.OnNext(result),
                ex => _exceptions.OnNext(ex));

            // RefCount is what ties cancellation to the subscription: dropping the last subscriber unsubscribes
            // from the body, which cancels the CancellationToken handed to a CreateFromTask command.
            return published.RefCount().ObserveOn(_outputScheduler);
        }
        catch (Exception ex)
        {
            _isExecuting.OnNext(false);
            _exceptions.OnNext(ex);
            return Observable.Throw<TResult>(ex);
        }
    }

    /// <summary>
    /// Subscribes to the results of every execution of this command.
    /// </summary>
    /// <param name="observer">The observer to notify.</param>
    /// <returns>A token that unsubscribes when disposed.</returns>
    public IDisposable Subscribe(IObserver<TResult> observer) =>
        _results.ObserveOn(_outputScheduler).Subscribe(observer);

    /// <inheritdoc/>
    bool ICommand.CanExecute(object? parameter) => _canExecuteLatest;

    /// <inheritdoc/>
    void ICommand.Execute(object? parameter) =>
        Execute(Coerce(parameter)).Subscribe(static _ => { }, static _ => { });

    /// <summary>
    /// Releases the command's subscriptions.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _canExecuteSubscription.Dispose();
        _isExecuting.Dispose();
        _results.Dispose();
        _exceptions.Dispose();
    }

    /// <summary>
    /// Converts the loosely typed parameter that <see cref="ICommand"/> hands us.
    /// </summary>
    /// <param name="parameter">The parameter supplied by the binding.</param>
    /// <returns>The typed parameter.</returns>
    private static TParam Coerce(object? parameter)
    {
        // A parameterless command should not blow up because the XAML happens to pass a CommandParameter.
        if (typeof(TParam) == typeof(Unit))
            return default!;

        return parameter switch
        {
            TParam typed => typed,
            null => default!,
            _ => throw new InvalidOperationException(
                $"A CommandParameter of type '{parameter.GetType()}' was passed to a command expecting " +
                $"'{typeof(TParam)}'."),
        };
    }
}