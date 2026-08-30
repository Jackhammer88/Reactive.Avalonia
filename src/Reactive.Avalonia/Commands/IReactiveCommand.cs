using System.Windows.Input;

namespace Reactive.Avalonia;

/// <summary>
/// The non-generic surface of <see cref="ReactiveCommand{TParam, TResult}"/>.
/// </summary>
/// <remarks>
/// Bind to this from XAML through <see cref="ICommand"/>; use the generic type in code when you care about the
/// parameter or result.
/// </remarks>
public interface IReactiveCommand : ICommand, IHandleObservableErrors, IDisposable
{
    /// <summary>
    /// Gets a sequence indicating whether the command can currently run.
    /// </summary>
    /// <remarks>
    /// A command is never executable while it is already executing. This deliberately hides
    /// <see cref="ICommand.CanExecute(object?)"/>, which stays reachable through an <see cref="ICommand"/>
    /// reference and is what the XAML binding machinery calls.
    /// </remarks>
    new IObservable<bool> CanExecute { get; }

    /// <summary>
    /// Gets a sequence indicating whether an execution is in flight.
    /// </summary>
    /// <remarks>Bind this to a progress indicator, or to <c>IsVisible</c> on a spinner.</remarks>
    IObservable<bool> IsExecuting { get; }
}