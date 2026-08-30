using System.Linq.Expressions;

namespace Reactive.Avalonia;

/// <summary>
/// Observes property changes as <see cref="IObservedChange{TSender, TValue}"/> notifications.
/// </summary>
/// <remarks>
/// Reach for this over <see cref="WhenAnyMixins.WhenAnyValue{TSender, T1}"/> when you need the sender or the
/// property name alongside the value, or when the current value should not be delivered on subscription.
/// </remarks>
public static class ObservableForPropertyMixins
{
    /// <summary>
    /// Observes a property, or a chain of properties, reporting each change as an observed change.
    /// </summary>
    /// <typeparam name="TSender">The type being observed.</typeparam>
    /// <typeparam name="TValue">The observed value type.</typeparam>
    /// <param name="sender">The object to observe.</param>
    /// <param name="property">A lambda shaped like <c>x =&gt; x.Foo</c> or <c>x =&gt; x.Foo.Bar</c>.</param>
    /// <param name="beforeChange">
    /// When <see langword="true"/>, notifications arrive just before the value changes and carry the outgoing
    /// value. Requires the object to implement <see cref="INotifyPropertyChanging"/>.
    /// </param>
    /// <param name="skipInitial">
    /// When <see langword="true"/> (the default), the current value is not delivered on subscription — only
    /// subsequent changes are.
    /// </param>
    /// <param name="isDistinct">
    /// When <see langword="true"/> (the default), consecutive identical values are dropped.
    /// </param>
    /// <returns>The observed changes.</returns>
    /// <example>
    /// <code language="csharp">
    /// person.ObservableForProperty(x => x.Name)
    ///       .Subscribe(change => Log($"{change.PropertyName} is now {change.Value}"));
    /// </code>
    /// </example>
    public static IObservable<IObservedChange<TSender, TValue>> ObservableForProperty<TSender, TValue>(
        this TSender sender,
        Expression<Func<TSender, TValue>> property,
        bool beforeChange = false,
        bool skipInitial = true,
        bool isDistinct = true)
        where TSender : class
    {
        ArgumentNullException.ThrowIfNull(sender);

        var chain = PropertyChain.Parse(property, nameof(property));
        var propertyName = chain[^1].Name;

        var values = PropertyChain.Observe(sender, chain, beforeChange);

        // Skip before dedupe, not after: the value emitted on subscription is by definition equal to the one
        // the first before-change notification reports, and collapsing them first would swallow that change.
        if (skipInitial)
        {
            values = values.Skip(1);
        }

        if (isDistinct)
        {
            values = values.DistinctUntilChanged();
        }

        return values.Select(value => (IObservedChange<TSender, TValue>)new ObservedChange<TSender, TValue>(
            sender,
            propertyName,
            value is TValue typed ? typed : default!));
    }

    /// <summary>
    /// Observes a property, or a chain of properties, projecting each change through a selector.
    /// </summary>
    /// <typeparam name="TSender">The type being observed.</typeparam>
    /// <typeparam name="TValue">The observed value type.</typeparam>
    /// <typeparam name="TRet">The projected type.</typeparam>
    /// <param name="sender">The object to observe.</param>
    /// <param name="property">A lambda shaped like <c>x =&gt; x.Foo</c> or <c>x =&gt; x.Foo.Bar</c>.</param>
    /// <param name="selector">Projects each observed change.</param>
    /// <param name="beforeChange">Whether notifications arrive before the change rather than after.</param>
    /// <param name="skipInitial">Whether the current value is skipped on subscription.</param>
    /// <param name="isDistinct">Whether consecutive identical values are dropped.</param>
    /// <returns>The projected values.</returns>
    public static IObservable<TRet> ObservableForProperty<TSender, TValue, TRet>(
        this TSender sender,
        Expression<Func<TSender, TValue>> property,
        Func<TValue, TRet> selector,
        bool beforeChange = false,
        bool skipInitial = true,
        bool isDistinct = true)
        where TSender : class
    {
        ArgumentNullException.ThrowIfNull(selector);
        return sender
            .ObservableForProperty(property, beforeChange, skipInitial, isDistinct)
            .Select(change => selector(change.Value));
    }
}