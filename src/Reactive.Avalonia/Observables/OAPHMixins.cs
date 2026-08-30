using System.Linq.Expressions;

namespace Reactive.Avalonia;

/// <summary>
/// Projects observable sequences onto read-only properties.
/// </summary>
/// <remarks>
/// Notifications are raised on <see cref="RxSchedulers.MainThreadScheduler"/> unless you say otherwise, which
/// is what makes it safe to drive a bound property from a background sequence.
/// </remarks>
/// <example>
/// <code language="csharp">
/// private readonly ObservableAsPropertyHelper&lt;string&gt; _greeting;
///
/// public MainViewModel()
/// {
///     this.WhenAnyValue(x => x.Name)
///         .Select(name => $"Hello {name}")
///         .ToProperty(this, x => x.Greeting, out _greeting);
/// }
///
/// public string Greeting => _greeting.Value;
/// </code>
/// </example>
public static class OAPHMixins
{
    /// <summary>Drives a read-only property from an observable sequence.</summary>
    /// <typeparam name="TObj">The object exposing the property.</typeparam>
    /// <typeparam name="TRet">The property type.</typeparam>
    /// <param name="source">The sequence to project.</param>
    /// <param name="owner">The object that raises the change notifications.</param>
    /// <param name="property">A lambda naming the property, shaped like <c>x =&gt; x.Foo</c>.</param>
    /// <param name="scheduler">The scheduler notifications are raised on.</param>
    /// <returns>The helper backing the property. Dispose it to unsubscribe.</returns>
    public static ObservableAsPropertyHelper<TRet> ToProperty<TObj, TRet>(
        this IObservable<TRet> source,
        TObj owner,
        Expression<Func<TObj, TRet>> property,
        IScheduler? scheduler = null)
        where TObj : class, IReactiveObject =>
        Create(source, owner, PropertyChain.SingleName(property, nameof(property)), default!, scheduler);

    /// <summary>Drives a read-only property from an observable sequence, starting from a known value.</summary>
    /// <typeparam name="TObj">The object exposing the property.</typeparam>
    /// <typeparam name="TRet">The property type.</typeparam>
    /// <param name="source">The sequence to project.</param>
    /// <param name="owner">The object that raises the change notifications.</param>
    /// <param name="property">A lambda naming the property, shaped like <c>x =&gt; x.Foo</c>.</param>
    /// <param name="initialValue">The value the property reports until <paramref name="source"/> produces one.</param>
    /// <param name="scheduler">The scheduler notifications are raised on.</param>
    /// <returns>The helper backing the property. Dispose it to unsubscribe.</returns>
    public static ObservableAsPropertyHelper<TRet> ToProperty<TObj, TRet>(
        this IObservable<TRet> source,
        TObj owner,
        Expression<Func<TObj, TRet>> property,
        TRet initialValue,
        IScheduler? scheduler = null)
        where TObj : class, IReactiveObject =>
        Create(source, owner, PropertyChain.SingleName(property, nameof(property)), initialValue, scheduler);

    /// <summary>Drives a read-only property, assigning the helper to a field in one step.</summary>
    /// <typeparam name="TObj">The object exposing the property.</typeparam>
    /// <typeparam name="TRet">The property type.</typeparam>
    /// <param name="source">The sequence to project.</param>
    /// <param name="owner">The object that raises the change notifications.</param>
    /// <param name="property">A lambda naming the property, shaped like <c>x =&gt; x.Foo</c>.</param>
    /// <param name="result">Receives the helper backing the property.</param>
    /// <param name="scheduler">The scheduler notifications are raised on.</param>
    /// <returns>The same helper that was assigned to <paramref name="result"/>.</returns>
    public static ObservableAsPropertyHelper<TRet> ToProperty<TObj, TRet>(
        this IObservable<TRet> source,
        TObj owner,
        Expression<Func<TObj, TRet>> property,
        out ObservableAsPropertyHelper<TRet> result,
        IScheduler? scheduler = null)
        where TObj : class, IReactiveObject =>
        result = Create(source, owner, PropertyChain.SingleName(property, nameof(property)), default!, scheduler);

    /// <summary>Drives a read-only property from a known value, assigning the helper to a field in one step.</summary>
    /// <typeparam name="TObj">The object exposing the property.</typeparam>
    /// <typeparam name="TRet">The property type.</typeparam>
    /// <param name="source">The sequence to project.</param>
    /// <param name="owner">The object that raises the change notifications.</param>
    /// <param name="property">A lambda naming the property, shaped like <c>x =&gt; x.Foo</c>.</param>
    /// <param name="result">Receives the helper backing the property.</param>
    /// <param name="initialValue">The value the property reports until <paramref name="source"/> produces one.</param>
    /// <param name="scheduler">The scheduler notifications are raised on.</param>
    /// <returns>The same helper that was assigned to <paramref name="result"/>.</returns>
    public static ObservableAsPropertyHelper<TRet> ToProperty<TObj, TRet>(
        this IObservable<TRet> source,
        TObj owner,
        Expression<Func<TObj, TRet>> property,
        out ObservableAsPropertyHelper<TRet> result,
        TRet initialValue,
        IScheduler? scheduler = null)
        where TObj : class, IReactiveObject =>
        result = Create(source, owner, PropertyChain.SingleName(property, nameof(property)), initialValue, scheduler);

    /// <summary>Drives a read-only property named directly rather than through a lambda.</summary>
    /// <typeparam name="TObj">The object exposing the property.</typeparam>
    /// <typeparam name="TRet">The property type.</typeparam>
    /// <param name="source">The sequence to project.</param>
    /// <param name="owner">The object that raises the change notifications.</param>
    /// <param name="propertyName">The property name, usually via <c>nameof</c>.</param>
    /// <param name="scheduler">The scheduler notifications are raised on.</param>
    /// <returns>The helper backing the property. Dispose it to unsubscribe.</returns>
    public static ObservableAsPropertyHelper<TRet> ToProperty<TObj, TRet>(
        this IObservable<TRet> source,
        TObj owner,
        string propertyName,
        IScheduler? scheduler = null)
        where TObj : class, IReactiveObject =>
        Create(source, owner, propertyName, default!, scheduler);

    /// <summary>Drives a read-only property named directly, starting from a known value.</summary>
    /// <typeparam name="TObj">The object exposing the property.</typeparam>
    /// <typeparam name="TRet">The property type.</typeparam>
    /// <param name="source">The sequence to project.</param>
    /// <param name="owner">The object that raises the change notifications.</param>
    /// <param name="propertyName">The property name, usually via <c>nameof</c>.</param>
    /// <param name="initialValue">The value the property reports until <paramref name="source"/> produces one.</param>
    /// <param name="scheduler">The scheduler notifications are raised on.</param>
    /// <returns>The helper backing the property. Dispose it to unsubscribe.</returns>
    public static ObservableAsPropertyHelper<TRet> ToProperty<TObj, TRet>(
        this IObservable<TRet> source,
        TObj owner,
        string propertyName,
        TRet initialValue,
        IScheduler? scheduler = null)
        where TObj : class, IReactiveObject =>
        Create(source, owner, propertyName, initialValue, scheduler);

    /// <summary>Drives a read-only property named directly, assigning the helper to a field in one step.</summary>
    /// <typeparam name="TObj">The object exposing the property.</typeparam>
    /// <typeparam name="TRet">The property type.</typeparam>
    /// <param name="source">The sequence to project.</param>
    /// <param name="owner">The object that raises the change notifications.</param>
    /// <param name="propertyName">The property name, usually via <c>nameof</c>.</param>
    /// <param name="result">Receives the helper backing the property.</param>
    /// <param name="scheduler">The scheduler notifications are raised on.</param>
    /// <returns>The same helper that was assigned to <paramref name="result"/>.</returns>
    public static ObservableAsPropertyHelper<TRet> ToProperty<TObj, TRet>(
        this IObservable<TRet> source,
        TObj owner,
        string propertyName,
        out ObservableAsPropertyHelper<TRet> result,
        IScheduler? scheduler = null)
        where TObj : class, IReactiveObject =>
        result = Create(source, owner, propertyName, default!, scheduler);

    /// <summary>
    /// Drives a read-only property named directly and starting from a known value, assigning the helper to a
    /// field in one step.
    /// </summary>
    /// <typeparam name="TObj">The object exposing the property.</typeparam>
    /// <typeparam name="TRet">The property type.</typeparam>
    /// <param name="source">The sequence to project.</param>
    /// <param name="owner">The object that raises the change notifications.</param>
    /// <param name="propertyName">The property name, usually via <c>nameof</c>.</param>
    /// <param name="result">Receives the helper backing the property.</param>
    /// <param name="initialValue">The value the property reports until <paramref name="source"/> produces one.</param>
    /// <param name="scheduler">The scheduler notifications are raised on.</param>
    /// <returns>The same helper that was assigned to <paramref name="result"/>.</returns>
    public static ObservableAsPropertyHelper<TRet> ToProperty<TObj, TRet>(
        this IObservable<TRet> source,
        TObj owner,
        string propertyName,
        out ObservableAsPropertyHelper<TRet> result,
        TRet initialValue,
        IScheduler? scheduler = null)
        where TObj : class, IReactiveObject =>
        result = Create(source, owner, propertyName, initialValue, scheduler);

    private static ObservableAsPropertyHelper<TRet> Create<TObj, TRet>(
        IObservable<TRet> source,
        TObj owner,
        string propertyName,
        TRet initialValue,
        IScheduler? scheduler)
        where TObj : class, IReactiveObject
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        return new ObservableAsPropertyHelper<TRet>(
            source,
            () => owner.RaisePropertyChanging(propertyName),
            () => owner.RaisePropertyChanged(propertyName),
            initialValue,
            scheduler ?? RxSchedulers.MainThreadScheduler);
    }
}