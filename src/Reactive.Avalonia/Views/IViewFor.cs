namespace Reactive.Avalonia;

/// <summary>
/// A view that shows a view model. The loosely typed half of <see cref="IViewFor{TViewModel}"/>.
/// </summary>
public interface IViewFor
{
    /// <summary>
    /// Gets or sets the view model being shown.
    /// </summary>
    object? ViewModel { get; set; }
}

/// <summary>
/// A view that shows a view model of a known type.
/// </summary>
/// <typeparam name="TViewModel">The view model type.</typeparam>
public interface IViewFor<TViewModel> : IViewFor
    where TViewModel : class
{
    /// <summary>
    /// Gets or sets the view model being shown.
    /// </summary>
    new TViewModel? ViewModel { get; set; }
}