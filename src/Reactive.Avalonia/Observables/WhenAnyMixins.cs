using System.Linq.Expressions;

namespace Reactive.Avalonia;

/// <summary>
/// Observes properties — and chains of properties — as observable sequences.
/// </summary>
/// <remarks>
/// <para>
/// Every overload emits the current value on subscription and then one value per change notification, and never
/// completes. Chains such as <c>x =&gt; x.Session.User.Name</c> re-subscribe automatically when a link in the
/// middle is replaced.
/// </para>
/// <para>
/// The lambdas are read, never compiled: nothing here calls <see cref="Expression{TDelegate}.Compile()"/>, so
/// these methods work under NativeAOT.
/// </para>
/// <para>
/// The object at the root of the chain must implement <see cref="INotifyPropertyChanged"/>. Without it there is
/// nothing to observe, and the sequence would produce one value and then sit silent forever — so this is a
/// compile error rather than a subscription that quietly stops working. Links in the middle of a chain are not
/// constrained: a plain object there is read once each time its owner hands out a new one, which is well
/// defined.
/// </para>
/// </remarks>
public static class WhenAnyMixins
{
    /// <summary>
    /// Observes a property, or a chain of properties, on <paramref name="sender"/>.
    /// </summary>
    /// <typeparam name="TSender">The type being observed.</typeparam>
    /// <typeparam name="T1">The observed value type.</typeparam>
    /// <param name="sender">The object to observe.</param>
    /// <param name="property1">A lambda shaped like <c>x =&gt; x.Foo</c> or <c>x =&gt; x.Foo.Bar</c>.</param>
    /// <returns>The current value, then every subsequent value.</returns>
    /// <example>
    /// <code language="csharp">
    /// this.WhenAnyValue(x => x.SearchText)
    ///     .Throttle(TimeSpan.FromMilliseconds(300))
    ///     .Subscribe(RunSearch);
    /// </code>
    /// </example>
    public static IObservable<T1> WhenAnyValue<TSender, T1>(
        this TSender sender,
        Expression<Func<TSender, T1>> property1)
        where TSender : class, INotifyPropertyChanged
    {
        ArgumentNullException.ThrowIfNull(sender);
        return Observe(sender, property1, nameof(property1));
    }

    /// <summary>
    /// Observes every property change on <paramref name="sender"/>, optionally narrowed to a set of names.
    /// </summary>
    /// <typeparam name="TSender">The type being observed.</typeparam>
    /// <param name="sender">The object to observe.</param>
    /// <param name="propertyNames">
    /// The property names to watch. Pass none to watch every property.
    /// </param>
    /// <returns>
    /// <paramref name="sender"/>, re-emitted on each matching change. Nothing is emitted on subscription.
    /// </returns>
    public static IObservable<TSender> WhenAnyPropertyChanged<TSender>(
        this TSender sender,
        params string[] propertyNames)
        where TSender : class, INotifyPropertyChanged
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(propertyNames);

        return Observable.Create<TSender>(observer =>
        {
            PropertyChangedEventHandler handler = (_, e) =>
            {
                if (propertyNames.Length == 0 ||
                    e.PropertyName is null ||
                    Array.IndexOf(propertyNames, e.PropertyName) >= 0)
                {
                    observer.OnNext(sender);
                }
            };

            sender.PropertyChanged += handler;
            return Disposable.Create(
                (sender, handler),
                static state => state.sender.PropertyChanged -= state.handler);
        });
    }


    /// <summary>
    /// Observes 2 properties and combines them with <paramref name="selector"/> whenever any of them changes.
    /// </summary>
    /// <typeparam name="TSender">The type being observed.</typeparam>
    /// <typeparam name="T1">The type of the first observed value.</typeparam>
    /// <typeparam name="T2">The type of the second observed value.</typeparam>
    /// <typeparam name="TRet">The combined result type.</typeparam>
    /// <param name="sender">The object to observe.</param>
    /// <param name="property1">A lambda selecting the first property.</param>
    /// <param name="property2">A lambda selecting the second property.</param>
    /// <param name="selector">Combines the latest value of each property.</param>
    /// <returns>The current combination, then one per change.</returns>
    public static IObservable<TRet> WhenAnyValue<TSender, T1, T2, TRet>(
        this TSender sender,
        Expression<Func<TSender, T1>> property1,
        Expression<Func<TSender, T2>> property2,
        Func<T1, T2, TRet> selector)
        where TSender : class, INotifyPropertyChanged
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(selector);
        return Observable.CombineLatest(
            Observe(sender, property1, nameof(property1)),
            Observe(sender, property2, nameof(property2)),
            selector);
    }

    /// <summary>
    /// Observes 3 properties and combines them with <paramref name="selector"/> whenever any of them changes.
    /// </summary>
    /// <typeparam name="TSender">The type being observed.</typeparam>
    /// <typeparam name="T1">The type of the first observed value.</typeparam>
    /// <typeparam name="T2">The type of the second observed value.</typeparam>
    /// <typeparam name="T3">The type of the third observed value.</typeparam>
    /// <typeparam name="TRet">The combined result type.</typeparam>
    /// <param name="sender">The object to observe.</param>
    /// <param name="property1">A lambda selecting the first property.</param>
    /// <param name="property2">A lambda selecting the second property.</param>
    /// <param name="property3">A lambda selecting the third property.</param>
    /// <param name="selector">Combines the latest value of each property.</param>
    /// <returns>The current combination, then one per change.</returns>
    public static IObservable<TRet> WhenAnyValue<TSender, T1, T2, T3, TRet>(
        this TSender sender,
        Expression<Func<TSender, T1>> property1,
        Expression<Func<TSender, T2>> property2,
        Expression<Func<TSender, T3>> property3,
        Func<T1, T2, T3, TRet> selector)
        where TSender : class, INotifyPropertyChanged
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(selector);
        return Observable.CombineLatest(
            Observe(sender, property1, nameof(property1)),
            Observe(sender, property2, nameof(property2)),
            Observe(sender, property3, nameof(property3)),
            selector);
    }

    /// <summary>
    /// Observes 4 properties and combines them with <paramref name="selector"/> whenever any of them changes.
    /// </summary>
    /// <typeparam name="TSender">The type being observed.</typeparam>
    /// <typeparam name="T1">The type of the first observed value.</typeparam>
    /// <typeparam name="T2">The type of the second observed value.</typeparam>
    /// <typeparam name="T3">The type of the third observed value.</typeparam>
    /// <typeparam name="T4">The type of the fourth observed value.</typeparam>
    /// <typeparam name="TRet">The combined result type.</typeparam>
    /// <param name="sender">The object to observe.</param>
    /// <param name="property1">A lambda selecting the first property.</param>
    /// <param name="property2">A lambda selecting the second property.</param>
    /// <param name="property3">A lambda selecting the third property.</param>
    /// <param name="property4">A lambda selecting the fourth property.</param>
    /// <param name="selector">Combines the latest value of each property.</param>
    /// <returns>The current combination, then one per change.</returns>
    public static IObservable<TRet> WhenAnyValue<TSender, T1, T2, T3, T4, TRet>(
        this TSender sender,
        Expression<Func<TSender, T1>> property1,
        Expression<Func<TSender, T2>> property2,
        Expression<Func<TSender, T3>> property3,
        Expression<Func<TSender, T4>> property4,
        Func<T1, T2, T3, T4, TRet> selector)
        where TSender : class, INotifyPropertyChanged
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(selector);
        return Observable.CombineLatest(
            Observe(sender, property1, nameof(property1)),
            Observe(sender, property2, nameof(property2)),
            Observe(sender, property3, nameof(property3)),
            Observe(sender, property4, nameof(property4)),
            selector);
    }

    /// <summary>
    /// Observes 5 properties and combines them with <paramref name="selector"/> whenever any of them changes.
    /// </summary>
    /// <typeparam name="TSender">The type being observed.</typeparam>
    /// <typeparam name="T1">The type of the first observed value.</typeparam>
    /// <typeparam name="T2">The type of the second observed value.</typeparam>
    /// <typeparam name="T3">The type of the third observed value.</typeparam>
    /// <typeparam name="T4">The type of the fourth observed value.</typeparam>
    /// <typeparam name="T5">The type of the fifth observed value.</typeparam>
    /// <typeparam name="TRet">The combined result type.</typeparam>
    /// <param name="sender">The object to observe.</param>
    /// <param name="property1">A lambda selecting the first property.</param>
    /// <param name="property2">A lambda selecting the second property.</param>
    /// <param name="property3">A lambda selecting the third property.</param>
    /// <param name="property4">A lambda selecting the fourth property.</param>
    /// <param name="property5">A lambda selecting the fifth property.</param>
    /// <param name="selector">Combines the latest value of each property.</param>
    /// <returns>The current combination, then one per change.</returns>
    public static IObservable<TRet> WhenAnyValue<TSender, T1, T2, T3, T4, T5, TRet>(
        this TSender sender,
        Expression<Func<TSender, T1>> property1,
        Expression<Func<TSender, T2>> property2,
        Expression<Func<TSender, T3>> property3,
        Expression<Func<TSender, T4>> property4,
        Expression<Func<TSender, T5>> property5,
        Func<T1, T2, T3, T4, T5, TRet> selector)
        where TSender : class, INotifyPropertyChanged
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(selector);
        return Observable.CombineLatest(
            Observe(sender, property1, nameof(property1)),
            Observe(sender, property2, nameof(property2)),
            Observe(sender, property3, nameof(property3)),
            Observe(sender, property4, nameof(property4)),
            Observe(sender, property5, nameof(property5)),
            selector);
    }

    /// <summary>
    /// Observes 6 properties and combines them with <paramref name="selector"/> whenever any of them changes.
    /// </summary>
    /// <typeparam name="TSender">The type being observed.</typeparam>
    /// <typeparam name="T1">The type of the first observed value.</typeparam>
    /// <typeparam name="T2">The type of the second observed value.</typeparam>
    /// <typeparam name="T3">The type of the third observed value.</typeparam>
    /// <typeparam name="T4">The type of the fourth observed value.</typeparam>
    /// <typeparam name="T5">The type of the fifth observed value.</typeparam>
    /// <typeparam name="T6">The type of the sixth observed value.</typeparam>
    /// <typeparam name="TRet">The combined result type.</typeparam>
    /// <param name="sender">The object to observe.</param>
    /// <param name="property1">A lambda selecting the first property.</param>
    /// <param name="property2">A lambda selecting the second property.</param>
    /// <param name="property3">A lambda selecting the third property.</param>
    /// <param name="property4">A lambda selecting the fourth property.</param>
    /// <param name="property5">A lambda selecting the fifth property.</param>
    /// <param name="property6">A lambda selecting the sixth property.</param>
    /// <param name="selector">Combines the latest value of each property.</param>
    /// <returns>The current combination, then one per change.</returns>
    public static IObservable<TRet> WhenAnyValue<TSender, T1, T2, T3, T4, T5, T6, TRet>(
        this TSender sender,
        Expression<Func<TSender, T1>> property1,
        Expression<Func<TSender, T2>> property2,
        Expression<Func<TSender, T3>> property3,
        Expression<Func<TSender, T4>> property4,
        Expression<Func<TSender, T5>> property5,
        Expression<Func<TSender, T6>> property6,
        Func<T1, T2, T3, T4, T5, T6, TRet> selector)
        where TSender : class, INotifyPropertyChanged
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(selector);
        return Observable.CombineLatest(
            Observe(sender, property1, nameof(property1)),
            Observe(sender, property2, nameof(property2)),
            Observe(sender, property3, nameof(property3)),
            Observe(sender, property4, nameof(property4)),
            Observe(sender, property5, nameof(property5)),
            Observe(sender, property6, nameof(property6)),
            selector);
    }

    /// <summary>
    /// Observes 7 properties and combines them with <paramref name="selector"/> whenever any of them changes.
    /// </summary>
    /// <typeparam name="TSender">The type being observed.</typeparam>
    /// <typeparam name="T1">The type of the first observed value.</typeparam>
    /// <typeparam name="T2">The type of the second observed value.</typeparam>
    /// <typeparam name="T3">The type of the third observed value.</typeparam>
    /// <typeparam name="T4">The type of the fourth observed value.</typeparam>
    /// <typeparam name="T5">The type of the fifth observed value.</typeparam>
    /// <typeparam name="T6">The type of the sixth observed value.</typeparam>
    /// <typeparam name="T7">The type of the seventh observed value.</typeparam>
    /// <typeparam name="TRet">The combined result type.</typeparam>
    /// <param name="sender">The object to observe.</param>
    /// <param name="property1">A lambda selecting the first property.</param>
    /// <param name="property2">A lambda selecting the second property.</param>
    /// <param name="property3">A lambda selecting the third property.</param>
    /// <param name="property4">A lambda selecting the fourth property.</param>
    /// <param name="property5">A lambda selecting the fifth property.</param>
    /// <param name="property6">A lambda selecting the sixth property.</param>
    /// <param name="property7">A lambda selecting the seventh property.</param>
    /// <param name="selector">Combines the latest value of each property.</param>
    /// <returns>The current combination, then one per change.</returns>
    public static IObservable<TRet> WhenAnyValue<TSender, T1, T2, T3, T4, T5, T6, T7, TRet>(
        this TSender sender,
        Expression<Func<TSender, T1>> property1,
        Expression<Func<TSender, T2>> property2,
        Expression<Func<TSender, T3>> property3,
        Expression<Func<TSender, T4>> property4,
        Expression<Func<TSender, T5>> property5,
        Expression<Func<TSender, T6>> property6,
        Expression<Func<TSender, T7>> property7,
        Func<T1, T2, T3, T4, T5, T6, T7, TRet> selector)
        where TSender : class, INotifyPropertyChanged
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(selector);
        return Observable.CombineLatest(
            Observe(sender, property1, nameof(property1)),
            Observe(sender, property2, nameof(property2)),
            Observe(sender, property3, nameof(property3)),
            Observe(sender, property4, nameof(property4)),
            Observe(sender, property5, nameof(property5)),
            Observe(sender, property6, nameof(property6)),
            Observe(sender, property7, nameof(property7)),
            selector);
    }

    /// <summary>
    /// Observes 8 properties and combines them with <paramref name="selector"/> whenever any of them changes.
    /// </summary>
    /// <typeparam name="TSender">The type being observed.</typeparam>
    /// <typeparam name="T1">The type of the first observed value.</typeparam>
    /// <typeparam name="T2">The type of the second observed value.</typeparam>
    /// <typeparam name="T3">The type of the third observed value.</typeparam>
    /// <typeparam name="T4">The type of the fourth observed value.</typeparam>
    /// <typeparam name="T5">The type of the fifth observed value.</typeparam>
    /// <typeparam name="T6">The type of the sixth observed value.</typeparam>
    /// <typeparam name="T7">The type of the seventh observed value.</typeparam>
    /// <typeparam name="T8">The type of the eighth observed value.</typeparam>
    /// <typeparam name="TRet">The combined result type.</typeparam>
    /// <param name="sender">The object to observe.</param>
    /// <param name="property1">A lambda selecting the first property.</param>
    /// <param name="property2">A lambda selecting the second property.</param>
    /// <param name="property3">A lambda selecting the third property.</param>
    /// <param name="property4">A lambda selecting the fourth property.</param>
    /// <param name="property5">A lambda selecting the fifth property.</param>
    /// <param name="property6">A lambda selecting the sixth property.</param>
    /// <param name="property7">A lambda selecting the seventh property.</param>
    /// <param name="property8">A lambda selecting the eighth property.</param>
    /// <param name="selector">Combines the latest value of each property.</param>
    /// <returns>The current combination, then one per change.</returns>
    public static IObservable<TRet> WhenAnyValue<TSender, T1, T2, T3, T4, T5, T6, T7, T8, TRet>(
        this TSender sender,
        Expression<Func<TSender, T1>> property1,
        Expression<Func<TSender, T2>> property2,
        Expression<Func<TSender, T3>> property3,
        Expression<Func<TSender, T4>> property4,
        Expression<Func<TSender, T5>> property5,
        Expression<Func<TSender, T6>> property6,
        Expression<Func<TSender, T7>> property7,
        Expression<Func<TSender, T8>> property8,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, TRet> selector)
        where TSender : class, INotifyPropertyChanged
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(selector);
        return Observable.CombineLatest(
            Observe(sender, property1, nameof(property1)),
            Observe(sender, property2, nameof(property2)),
            Observe(sender, property3, nameof(property3)),
            Observe(sender, property4, nameof(property4)),
            Observe(sender, property5, nameof(property5)),
            Observe(sender, property6, nameof(property6)),
            Observe(sender, property7, nameof(property7)),
            Observe(sender, property8, nameof(property8)),
            selector);
    }

    /// <summary>
    /// Observes 2 properties and emits them as a tuple whenever any of them changes.
    /// </summary>
    /// <typeparam name="TSender">The type being observed.</typeparam>
    /// <typeparam name="T1">The type of the first observed value.</typeparam>
    /// <typeparam name="T2">The type of the second observed value.</typeparam>
    /// <param name="sender">The object to observe.</param>
    /// <param name="property1">A lambda selecting the first property.</param>
    /// <param name="property2">A lambda selecting the second property.</param>
    /// <returns>The current values, then a tuple per change.</returns>
    public static IObservable<(T1, T2)> WhenAnyValue<TSender, T1, T2>(
        this TSender sender,
        Expression<Func<TSender, T1>> property1,
        Expression<Func<TSender, T2>> property2)
        where TSender : class, INotifyPropertyChanged
    {
        ArgumentNullException.ThrowIfNull(sender);
        return Observable.CombineLatest(
            Observe(sender, property1, nameof(property1)),
            Observe(sender, property2, nameof(property2)),
            static (v1, v2) => (v1, v2));
    }

    /// <summary>
    /// Observes 3 properties and emits them as a tuple whenever any of them changes.
    /// </summary>
    /// <typeparam name="TSender">The type being observed.</typeparam>
    /// <typeparam name="T1">The type of the first observed value.</typeparam>
    /// <typeparam name="T2">The type of the second observed value.</typeparam>
    /// <typeparam name="T3">The type of the third observed value.</typeparam>
    /// <param name="sender">The object to observe.</param>
    /// <param name="property1">A lambda selecting the first property.</param>
    /// <param name="property2">A lambda selecting the second property.</param>
    /// <param name="property3">A lambda selecting the third property.</param>
    /// <returns>The current values, then a tuple per change.</returns>
    public static IObservable<(T1, T2, T3)> WhenAnyValue<TSender, T1, T2, T3>(
        this TSender sender,
        Expression<Func<TSender, T1>> property1,
        Expression<Func<TSender, T2>> property2,
        Expression<Func<TSender, T3>> property3)
        where TSender : class, INotifyPropertyChanged
    {
        ArgumentNullException.ThrowIfNull(sender);
        return Observable.CombineLatest(
            Observe(sender, property1, nameof(property1)),
            Observe(sender, property2, nameof(property2)),
            Observe(sender, property3, nameof(property3)),
            static (v1, v2, v3) => (v1, v2, v3));
    }

    /// <summary>
    /// Observes 4 properties and emits them as a tuple whenever any of them changes.
    /// </summary>
    /// <typeparam name="TSender">The type being observed.</typeparam>
    /// <typeparam name="T1">The type of the first observed value.</typeparam>
    /// <typeparam name="T2">The type of the second observed value.</typeparam>
    /// <typeparam name="T3">The type of the third observed value.</typeparam>
    /// <typeparam name="T4">The type of the fourth observed value.</typeparam>
    /// <param name="sender">The object to observe.</param>
    /// <param name="property1">A lambda selecting the first property.</param>
    /// <param name="property2">A lambda selecting the second property.</param>
    /// <param name="property3">A lambda selecting the third property.</param>
    /// <param name="property4">A lambda selecting the fourth property.</param>
    /// <returns>The current values, then a tuple per change.</returns>
    public static IObservable<(T1, T2, T3, T4)> WhenAnyValue<TSender, T1, T2, T3, T4>(
        this TSender sender,
        Expression<Func<TSender, T1>> property1,
        Expression<Func<TSender, T2>> property2,
        Expression<Func<TSender, T3>> property3,
        Expression<Func<TSender, T4>> property4)
        where TSender : class, INotifyPropertyChanged
    {
        ArgumentNullException.ThrowIfNull(sender);
        return Observable.CombineLatest(
            Observe(sender, property1, nameof(property1)),
            Observe(sender, property2, nameof(property2)),
            Observe(sender, property3, nameof(property3)),
            Observe(sender, property4, nameof(property4)),
            static (v1, v2, v3, v4) => (v1, v2, v3, v4));
    }

    /// <summary>
    /// Observes 5 properties and emits them as a tuple whenever any of them changes.
    /// </summary>
    /// <typeparam name="TSender">The type being observed.</typeparam>
    /// <typeparam name="T1">The type of the first observed value.</typeparam>
    /// <typeparam name="T2">The type of the second observed value.</typeparam>
    /// <typeparam name="T3">The type of the third observed value.</typeparam>
    /// <typeparam name="T4">The type of the fourth observed value.</typeparam>
    /// <typeparam name="T5">The type of the fifth observed value.</typeparam>
    /// <param name="sender">The object to observe.</param>
    /// <param name="property1">A lambda selecting the first property.</param>
    /// <param name="property2">A lambda selecting the second property.</param>
    /// <param name="property3">A lambda selecting the third property.</param>
    /// <param name="property4">A lambda selecting the fourth property.</param>
    /// <param name="property5">A lambda selecting the fifth property.</param>
    /// <returns>The current values, then a tuple per change.</returns>
    public static IObservable<(T1, T2, T3, T4, T5)> WhenAnyValue<TSender, T1, T2, T3, T4, T5>(
        this TSender sender,
        Expression<Func<TSender, T1>> property1,
        Expression<Func<TSender, T2>> property2,
        Expression<Func<TSender, T3>> property3,
        Expression<Func<TSender, T4>> property4,
        Expression<Func<TSender, T5>> property5)
        where TSender : class, INotifyPropertyChanged
    {
        ArgumentNullException.ThrowIfNull(sender);
        return Observable.CombineLatest(
            Observe(sender, property1, nameof(property1)),
            Observe(sender, property2, nameof(property2)),
            Observe(sender, property3, nameof(property3)),
            Observe(sender, property4, nameof(property4)),
            Observe(sender, property5, nameof(property5)),
            static (v1, v2, v3, v4, v5) => (v1, v2, v3, v4, v5));
    }

    /// <summary>
    /// Builds the sequence for one property lambda.
    /// </summary>
    /// <typeparam name="TSender">The type being observed.</typeparam>
    /// <typeparam name="TValue">The observed value type.</typeparam>
    /// <param name="sender">The object to observe.</param>
    /// <param name="property">The property lambda.</param>
    /// <param name="parameterName">The caller's argument name, used in exception messages.</param>
    /// <returns>The observed values.</returns>
    private static IObservable<TValue> Observe<TSender, TValue>(
        TSender sender,
        Expression<Func<TSender, TValue>> property,
        string parameterName)
        where TSender : class, INotifyPropertyChanged
    {
        var chain = PropertyChain.Parse(property, parameterName);
        var propertyName = chain[^1].Name;

        return PropertyChain
            .Observe(sender, chain)
            .Select(value => PropertyChain.Convert<TValue>(value, propertyName));
    }
}