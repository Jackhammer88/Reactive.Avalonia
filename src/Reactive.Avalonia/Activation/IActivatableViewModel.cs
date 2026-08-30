namespace Reactive.Avalonia;

/// <summary>
/// A view model whose subscriptions are tied to the lifetime of the view showing it.
/// </summary>
/// <remarks>
/// Implement this and expose a <see cref="ViewModelActivator"/>, then set up subscriptions inside
/// <c>this.WhenActivated(...)</c>. They are torn down when the view leaves the visual tree and rebuilt if it
/// comes back — which is what stops a long-lived view model from leaking subscriptions into dead views.
/// </remarks>
/// <example>
/// <code language="csharp">
/// public sealed class SearchViewModel : ReactiveObject, IActivatableViewModel
/// {
///     public ViewModelActivator Activator { get; } = new();
///
///     public SearchViewModel() =>
///         this.WhenActivated(disposables =>
///             _timer.Subscribe(Refresh).DisposeWith(disposables));
/// }
/// </code>
/// </example>
public interface IActivatableViewModel
{
    /// <summary>
    /// Gets the object that tracks this view model's activation.
    /// </summary>
    ViewModelActivator Activator { get; }
}