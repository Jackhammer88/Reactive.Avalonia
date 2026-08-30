namespace Reactive.Avalonia;

/// <summary>
/// The property-setting helper, for types that implement <see cref="IReactiveObject"/> directly instead of
/// inheriting <see cref="ReactiveObject"/>.
/// </summary>
public static class ReactiveObjectExtensions
{
    /// <summary>
    /// Assigns <paramref name="newValue"/> to <paramref name="backingField"/> and raises change notifications,
    /// but only when the value actually differs.
    /// </summary>
    /// <typeparam name="TObj">The object raising the notifications.</typeparam>
    /// <typeparam name="TRet">The property type.</typeparam>
    /// <param name="reactiveObject">The object raising the notifications.</param>
    /// <param name="backingField">The backing field to assign to.</param>
    /// <param name="newValue">The new value.</param>
    /// <param name="propertyName">The property name. Supplied by the compiler when omitted.</param>
    /// <returns>The value now held by the property.</returns>
    /// <example>
    /// <code language="csharp">
    /// public string Name
    /// {
    ///     get => _name;
    ///     set => this.RaiseAndSetIfChanged(ref _name, value);
    /// }
    /// </code>
    /// </example>
    public static TRet RaiseAndSetIfChanged<TObj, TRet>(
        this TObj reactiveObject,
        ref TRet backingField,
        TRet newValue,
        [CallerMemberName] string? propertyName = null)
        where TObj : IReactiveObject
    {
        ArgumentNullException.ThrowIfNull(reactiveObject);
        ArgumentNullException.ThrowIfNull(propertyName);

        if (EqualityComparer<TRet>.Default.Equals(backingField, newValue))
        {
            return newValue;
        }

        reactiveObject.RaisePropertyChanging(propertyName);
        backingField = newValue;
        reactiveObject.RaisePropertyChanged(propertyName);
        return newValue;
    }
}