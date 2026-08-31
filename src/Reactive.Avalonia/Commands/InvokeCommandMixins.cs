using System.Linq.Expressions;
using System.Windows.Input;

namespace Reactive.Avalonia;

/// <summary>
/// Routes the values of an observable sequence into a command.
/// </summary>
/// <remarks>
/// Values that arrive while the command cannot execute are dropped, not queued — the command's
/// <c>canExecute</c> gate is respected rather than worked around.
/// </remarks>
/// <example>
/// <code language="csharp">
/// this.WhenAnyValue(x => x.SearchText)
///     .Throttle(TimeSpan.FromMilliseconds(300))
///     .InvokeCommand(this, x => x.Search);
/// </code>
/// </example>
public static class InvokeCommandMixins
{
    /// <summary>
    /// Executes <paramref name="command"/> for each value, passing the value as the parameter.
    /// </summary>
    /// <typeparam name="T">The element type, used as the command parameter.</typeparam>
    /// <param name="source">The sequence driving the command.</param>
    /// <param name="command">The command to run.</param>
    /// <returns>A token that stops driving the command when disposed.</returns>
    public static IDisposable InvokeCommand<T>(this IObservable<T> source, ICommand? command)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Subscribe(value =>
        {
            if (command is not null && command.CanExecute(value))
            {
                command.Execute(value);
            }
        });
    }

    /// <summary>
    /// Executes <paramref name="command"/> for each value, passing the value as the parameter.
    /// </summary>
    /// <typeparam name="TParam">The command parameter type.</typeparam>
    /// <typeparam name="TResult">The command result type.</typeparam>
    /// <param name="source">The sequence driving the command.</param>
    /// <param name="command">The command to run.</param>
    /// <returns>A token that stops driving the command when disposed.</returns>
    /// <remarks>
    /// Failures reach the command's <see cref="IHandleObservableErrors.ThrownExceptions"/>, so they do not tear
    /// down this subscription.
    /// </remarks>
    public static IDisposable InvokeCommand<TParam, TResult>(
        this IObservable<TParam> source,
        ReactiveCommand<TParam, TResult>? command)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (command is null)
        {
            return Disposable.Empty;
        }

        return source
            .WithLatestFrom(command.CanExecute, static (value, canExecute) => (value, canExecute))
            .Where(static pair => pair.canExecute)
            .SelectMany(pair => command.Execute(pair.value).Catch(Observable.Empty<TResult>()))
            .Subscribe();
    }

    /// <summary>
    /// Executes the command found at <paramref name="commandProperty"/> for each value.
    /// </summary>
    /// <typeparam name="T">The element type, used as the command parameter.</typeparam>
    /// <typeparam name="TTarget">The object holding the command.</typeparam>
    /// <param name="source">The sequence driving the command.</param>
    /// <param name="target">The object holding the command.</param>
    /// <param name="commandProperty">A lambda selecting the command property.</param>
    /// <returns>A token that stops driving the command when disposed.</returns>
    /// <remarks>
    /// The command is looked up per value, so reassigning the property redirects subsequent values.
    /// </remarks>
    public static IDisposable InvokeCommand<T, TTarget>(
        this IObservable<T> source,
        TTarget target,
        Expression<Func<TTarget, ICommand?>> commandProperty)
        where TTarget : class, INotifyPropertyChanged
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        return source
            .WithLatestFrom(target.WhenAnyValue(commandProperty), static (value, command) => (value, command))
            .Subscribe(pair =>
            {
                if (pair.command is not null && pair.command.CanExecute(pair.value))
                {
                    pair.command.Execute(pair.value);
                }
            });
    }
}