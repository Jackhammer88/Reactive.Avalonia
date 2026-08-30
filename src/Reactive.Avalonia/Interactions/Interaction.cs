namespace Reactive.Avalonia;

/// <summary>
/// Lets a view model ask the view a question and wait for the answer.
/// </summary>
/// <typeparam name="TInput">What the view model is asking about.</typeparam>
/// <typeparam name="TOutput">What it needs back.</typeparam>
/// <remarks>
/// This is how a view model opens a dialog without knowing what a dialog is. The view model exposes an
/// interaction and awaits it; the view registers a handler that actually shows something. Handlers are
/// consulted newest first, and the first one to call
/// <see cref="InteractionContext{TInput, TOutput}.SetOutput"/> wins.
/// </remarks>
/// <example>
/// <code language="csharp">
/// // view model
/// public Interaction&lt;string, bool&gt; Confirm { get; } = new();
///
/// private async Task DeleteAsync() =>
///     _ = await Confirm.Handle("Delete this file?");
///
/// // view
/// this.WhenActivated(disposables =>
///     ViewModel!.Confirm.RegisterHandler(async context =>
///         context.SetOutput(await ShowDialogAsync(context.Input)))
///              .DisposeWith(disposables));
/// </code>
/// </example>
public class Interaction<TInput, TOutput>
{
    private readonly List<Func<InteractionContext<TInput, TOutput>, IObservable<Unit>>> _handlers = [];
    private readonly Lock _gate = new();
    private readonly IScheduler? _handlerScheduler;

    /// <summary>
    /// Initializes a new instance of the <see cref="Interaction{TInput, TOutput}"/> class whose handlers run
    /// on whichever thread calls <see cref="Handle"/>.
    /// </summary>
    public Interaction()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Interaction{TInput, TOutput}"/> class whose handlers run on
    /// a specific scheduler.
    /// </summary>
    /// <param name="handlerScheduler">The scheduler to invoke handlers on.</param>
    /// <remarks>
    /// Pass <see cref="RxSchedulers.MainThreadScheduler"/> when the view model may raise the interaction from a
    /// background thread but the handler needs to touch the UI.
    /// </remarks>
    public Interaction(IScheduler handlerScheduler)
    {
        ArgumentNullException.ThrowIfNull(handlerScheduler);
        _handlerScheduler = handlerScheduler;
    }

    /// <summary>
    /// Registers a synchronous handler.
    /// </summary>
    /// <param name="handler">Answers the interaction.</param>
    /// <returns>A token that unregisters the handler when disposed.</returns>
    public IDisposable RegisterHandler(Action<InteractionContext<TInput, TOutput>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return RegisterHandler(context =>
        {
            handler(context);
            return Observable.Return(Unit.Default);
        });
    }

    /// <summary>
    /// Registers an asynchronous handler.
    /// </summary>
    /// <param name="handler">Answers the interaction.</param>
    /// <returns>A token that unregisters the handler when disposed.</returns>
    public IDisposable RegisterHandler(Func<InteractionContext<TInput, TOutput>, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return RegisterHandler(context => Observable.FromAsync(() => handler(context)));
    }

    /// <summary>
    /// Registers a handler whose work is an observable sequence.
    /// </summary>
    /// <param name="handler">Answers the interaction.</param>
    /// <returns>A token that unregisters the handler when disposed.</returns>
    public IDisposable RegisterHandler(Func<InteractionContext<TInput, TOutput>, IObservable<Unit>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (_gate)
        {
            _handlers.Add(handler);
        }

        return Disposable.Create((self: this, handler), static state =>
        {
            lock (state.self._gate)
            {
                state.self._handlers.Remove(state.handler);
            }
        });
    }

    /// <summary>
    /// Asks the registered handlers, newest first, until one answers.
    /// </summary>
    /// <param name="input">The value to ask about.</param>
    /// <returns>The answer.</returns>
    /// <exception cref="UnhandledInteractionException">No handler answered.</exception>
    public IObservable<TOutput> Handle(TInput input)
    {
        Func<InteractionContext<TInput, TOutput>, IObservable<Unit>>[] handlers;

        lock (_gate)
        {
            handlers = [.. _handlers];
        }

        return Observable.Defer(() =>
        {
            var context = new InteractionContext<TInput, TOutput>(input);

            // Newest handler first; once one answers, the rest short-circuit to Empty.
            var chain = Observable.Empty<Unit>();
            for (var i = handlers.Length - 1; i >= 0; i--)
            {
                var handler = handlers[i];
                var invoke = Observable.Defer(() =>
                    context.IsHandled ? Observable.Empty<Unit>() : handler(context));

                // No scheduler by default: handlers run inline, which keeps a nested Handle() from queueing
                // behind the one that triggered it.
                chain = chain.Concat(_handlerScheduler is null ? invoke : invoke.SubscribeOn(_handlerScheduler));
            }

            return chain
                .IgnoreElements()
                .Select(static _ => default(TOutput)!)
                .Concat(Observable.Defer(() => context.IsHandled
                    ? Observable.Return(context.GetOutput())
                    : Observable.Throw<TOutput>(new UnhandledInteractionException(
                        $"No handler answered the interaction for '{input}'. Register one from the view, " +
                        "usually inside a WhenActivated block."))));
        });
    }
}