namespace Reactive.Avalonia;

/// <summary>
/// A single observed property change: which object, which property, and the value.
/// </summary>
/// <typeparam name="TSender">The type of the object the property lives on.</typeparam>
/// <typeparam name="TValue">The property type.</typeparam>
public interface IObservedChange<out TSender, out TValue>
{
    /// <summary>
    /// Gets the object the observed chain started at.
    /// </summary>
    TSender Sender { get; }

    /// <summary>
    /// Gets the name of the property that changed — the last link of the chain.
    /// </summary>
    string PropertyName { get; }

    /// <summary>
    /// Gets the value. For a before-change notification this is the value about to be replaced.
    /// </summary>
    TValue Value { get; }
}

/// <summary>
/// The default <see cref="IObservedChange{TSender, TValue}"/> implementation.
/// </summary>
/// <typeparam name="TSender">The type of the object the property lives on.</typeparam>
/// <typeparam name="TValue">The property type.</typeparam>
/// <param name="Sender">The object the observed chain started at.</param>
/// <param name="PropertyName">The name of the property that changed.</param>
/// <param name="Value">The value.</param>
public sealed record ObservedChange<TSender, TValue>(TSender Sender, string PropertyName, TValue Value)
    : IObservedChange<TSender, TValue>;