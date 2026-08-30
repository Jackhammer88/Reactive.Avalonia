namespace Reactive.Avalonia;

/// <summary>
/// A view model that carries validation rules.
/// </summary>
/// <remarks>
/// Inherit from <see cref="ReactiveValidationObject"/> to get this plus the
/// <see cref="INotifyDataErrorInfo"/> plumbing that Avalonia reads to show errors in the UI.
/// </remarks>
public interface IValidatableViewModel
{
    /// <summary>
    /// Gets the rules registered for this view model.
    /// </summary>
    ValidationContext ValidationContext { get; }
}