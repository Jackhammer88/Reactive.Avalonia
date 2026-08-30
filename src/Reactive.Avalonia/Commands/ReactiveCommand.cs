using System.Threading;

namespace Reactive.Avalonia;

/// <summary>
/// Creates <see cref="ReactiveCommand{TParam, TResult}"/> instances.
/// </summary>
/// <remarks>
/// Every factory takes an optional <c>canExecute</c> sequence — usually a <c>WhenAnyValue</c> chain — and an
/// optional output scheduler. Commands are disabled automatically while they are executing.
/// </remarks>
/// <example>
/// <code language="csharp">
/// var canSave = this.WhenAnyValue(x => x.Name, name => !string.IsNullOrWhiteSpace(name));
/// Save = ReactiveCommand.CreateFromTask(SaveAsync, canSave);
/// </code>
/// </example>
public static class ReactiveCommand
{
    /// <summary>Creates a command that runs a synchronous action.</summary>
    /// <param name="execute">The command body.</param>
    /// <param name="canExecute">Gates execution.</param>
    /// <param name="outputScheduler">The scheduler state changes are delivered on.</param>
    /// <returns>The command.</returns>
    public static ReactiveCommand<Unit, Unit> Create(
        Action execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        return new ReactiveCommand<Unit, Unit>(
            _ => Observable.Create<Unit>(observer =>
            {
                execute();
                observer.OnNext(Unit.Default);
                observer.OnCompleted();
                return Disposable.Empty;
            }),
            canExecute,
            outputScheduler);
    }

    /// <summary>Creates a command that runs a synchronous action taking a parameter.</summary>
    /// <typeparam name="TParam">The parameter type.</typeparam>
    /// <param name="execute">The command body.</param>
    /// <param name="canExecute">Gates execution.</param>
    /// <param name="outputScheduler">The scheduler state changes are delivered on.</param>
    /// <returns>The command.</returns>
    public static ReactiveCommand<TParam, Unit> Create<TParam>(
        Action<TParam> execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        return new ReactiveCommand<TParam, Unit>(
            parameter => Observable.Create<Unit>(observer =>
            {
                execute(parameter);
                observer.OnNext(Unit.Default);
                observer.OnCompleted();
                return Disposable.Empty;
            }),
            canExecute,
            outputScheduler);
    }

    /// <summary>Creates a command that runs a synchronous function.</summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="execute">The command body.</param>
    /// <param name="canExecute">Gates execution.</param>
    /// <param name="outputScheduler">The scheduler results are delivered on.</param>
    /// <returns>The command.</returns>
    public static ReactiveCommand<Unit, TResult> Create<TResult>(
        Func<TResult> execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        return new ReactiveCommand<Unit, TResult>(
            _ => Observable.Create<TResult>(observer =>
            {
                observer.OnNext(execute());
                observer.OnCompleted();
                return Disposable.Empty;
            }),
            canExecute,
            outputScheduler);
    }

    /// <summary>Creates a command that runs a synchronous function taking a parameter.</summary>
    /// <typeparam name="TParam">The parameter type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="execute">The command body.</param>
    /// <param name="canExecute">Gates execution.</param>
    /// <param name="outputScheduler">The scheduler results are delivered on.</param>
    /// <returns>The command.</returns>
    public static ReactiveCommand<TParam, TResult> Create<TParam, TResult>(
        Func<TParam, TResult> execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        return new ReactiveCommand<TParam, TResult>(
            parameter => Observable.Create<TResult>(observer =>
            {
                observer.OnNext(execute(parameter));
                observer.OnCompleted();
                return Disposable.Empty;
            }),
            canExecute,
            outputScheduler);
    }

    /// <summary>Creates a command from an asynchronous method.</summary>
    /// <param name="execute">The command body.</param>
    /// <param name="canExecute">Gates execution.</param>
    /// <param name="outputScheduler">The scheduler state changes are delivered on.</param>
    /// <returns>The command.</returns>
    public static ReactiveCommand<Unit, Unit> CreateFromTask(
        Func<Task> execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        return new ReactiveCommand<Unit, Unit>(_ => Observable.FromAsync(execute), canExecute, outputScheduler);
    }

    /// <summary>Creates a command from a cancellable asynchronous method.</summary>
    /// <param name="execute">The command body. The token is cancelled when the subscription is disposed.</param>
    /// <param name="canExecute">Gates execution.</param>
    /// <param name="outputScheduler">The scheduler state changes are delivered on.</param>
    /// <returns>The command.</returns>
    public static ReactiveCommand<Unit, Unit> CreateFromTask(
        Func<CancellationToken, Task> execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        return new ReactiveCommand<Unit, Unit>(_ => Observable.FromAsync(execute), canExecute, outputScheduler);
    }

    /// <summary>Creates a command from an asynchronous function.</summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="execute">The command body.</param>
    /// <param name="canExecute">Gates execution.</param>
    /// <param name="outputScheduler">The scheduler results are delivered on.</param>
    /// <returns>The command.</returns>
    public static ReactiveCommand<Unit, TResult> CreateFromTask<TResult>(
        Func<Task<TResult>> execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        return new ReactiveCommand<Unit, TResult>(_ => Observable.FromAsync(execute), canExecute, outputScheduler);
    }

    /// <summary>Creates a command from a cancellable asynchronous function.</summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="execute">The command body. The token is cancelled when the subscription is disposed.</param>
    /// <param name="canExecute">Gates execution.</param>
    /// <param name="outputScheduler">The scheduler results are delivered on.</param>
    /// <returns>The command.</returns>
    public static ReactiveCommand<Unit, TResult> CreateFromTask<TResult>(
        Func<CancellationToken, Task<TResult>> execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        return new ReactiveCommand<Unit, TResult>(_ => Observable.FromAsync(execute), canExecute, outputScheduler);
    }

    /// <summary>Creates a command from an asynchronous method taking a parameter.</summary>
    /// <typeparam name="TParam">The parameter type.</typeparam>
    /// <param name="execute">The command body.</param>
    /// <param name="canExecute">Gates execution.</param>
    /// <param name="outputScheduler">The scheduler state changes are delivered on.</param>
    /// <returns>The command.</returns>
    public static ReactiveCommand<TParam, Unit> CreateFromTask<TParam>(
        Func<TParam, Task> execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        return new ReactiveCommand<TParam, Unit>(
            parameter => Observable.FromAsync(() => execute(parameter)),
            canExecute,
            outputScheduler);
    }

    /// <summary>Creates a command from a cancellable asynchronous method taking a parameter.</summary>
    /// <typeparam name="TParam">The parameter type.</typeparam>
    /// <param name="execute">The command body. The token is cancelled when the subscription is disposed.</param>
    /// <param name="canExecute">Gates execution.</param>
    /// <param name="outputScheduler">The scheduler state changes are delivered on.</param>
    /// <returns>The command.</returns>
    public static ReactiveCommand<TParam, Unit> CreateFromTask<TParam>(
        Func<TParam, CancellationToken, Task> execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        return new ReactiveCommand<TParam, Unit>(
            parameter => Observable.FromAsync(token => execute(parameter, token)),
            canExecute,
            outputScheduler);
    }

    /// <summary>Creates a command from an asynchronous function taking a parameter.</summary>
    /// <typeparam name="TParam">The parameter type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="execute">The command body.</param>
    /// <param name="canExecute">Gates execution.</param>
    /// <param name="outputScheduler">The scheduler results are delivered on.</param>
    /// <returns>The command.</returns>
    public static ReactiveCommand<TParam, TResult> CreateFromTask<TParam, TResult>(
        Func<TParam, Task<TResult>> execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        return new ReactiveCommand<TParam, TResult>(
            parameter => Observable.FromAsync(() => execute(parameter)),
            canExecute,
            outputScheduler);
    }

    /// <summary>Creates a command from a cancellable asynchronous function taking a parameter.</summary>
    /// <typeparam name="TParam">The parameter type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="execute">The command body. The token is cancelled when the subscription is disposed.</param>
    /// <param name="canExecute">Gates execution.</param>
    /// <param name="outputScheduler">The scheduler results are delivered on.</param>
    /// <returns>The command.</returns>
    public static ReactiveCommand<TParam, TResult> CreateFromTask<TParam, TResult>(
        Func<TParam, CancellationToken, Task<TResult>> execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        return new ReactiveCommand<TParam, TResult>(
            parameter => Observable.FromAsync(token => execute(parameter, token)),
            canExecute,
            outputScheduler);
    }

    /// <summary>Creates a command whose body is an observable sequence.</summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="execute">Produces the sequence for one execution.</param>
    /// <param name="canExecute">Gates execution.</param>
    /// <param name="outputScheduler">The scheduler results are delivered on.</param>
    /// <returns>The command.</returns>
    public static ReactiveCommand<Unit, TResult> CreateFromObservable<TResult>(
        Func<IObservable<TResult>> execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        return new ReactiveCommand<Unit, TResult>(_ => execute(), canExecute, outputScheduler);
    }

    /// <summary>Creates a command whose body is an observable sequence, taking a parameter.</summary>
    /// <typeparam name="TParam">The parameter type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="execute">Produces the sequence for one execution.</param>
    /// <param name="canExecute">Gates execution.</param>
    /// <param name="outputScheduler">The scheduler results are delivered on.</param>
    /// <returns>The command.</returns>
    public static ReactiveCommand<TParam, TResult> CreateFromObservable<TParam, TResult>(
        Func<TParam, IObservable<TResult>> execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        return new ReactiveCommand<TParam, TResult>(execute, canExecute, outputScheduler);
    }
}