namespace Reactive.Avalonia;

/// <summary>
/// Represents an object that raises both change and changing notifications, and exposes them as observables.
/// </summary>
/// <remarks>
/// Inherit from <see cref="ReactiveObject"/> unless the type already has a mandatory base class, in which
/// case implement this interface directly. Everything in this library that observes properties only needs
/// <see cref="INotifyPropertyChanged"/>, so a hand-rolled implementation works everywhere too.
/// </remarks>
public interface IReactiveObject : INotifyPropertyChanged, INotifyPropertyChanging
{
    /// <summary>
    /// Gets a sequence of property names that have just changed.
    /// </summary>
    IObservable<PropertyChangedEventArgs> Changed { get; }

    /// <summary>
    /// Gets a sequence of property names that are about to change.
    /// </summary>
    IObservable<PropertyChangingEventArgs> Changing { get; }

    /// <summary>
    /// Raises a change notification for the given property.
    /// </summary>
    /// <param name="propertyName">The property that changed. Supplied by the compiler when omitted.</param>
    void RaisePropertyChanged([CallerMemberName] string? propertyName = null);

    /// <summary>
    /// Raises a changing notification for the given property.
    /// </summary>
    /// <param name="propertyName">The property that is about to change. Supplied by the compiler when omitted.</param>
    void RaisePropertyChanging([CallerMemberName] string? propertyName = null);
}