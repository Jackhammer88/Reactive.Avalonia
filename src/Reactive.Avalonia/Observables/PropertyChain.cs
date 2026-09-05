using System.Linq.Expressions;
using System.Reflection;

namespace Reactive.Avalonia;

/// <summary>
/// One property access inside a <c>x =&gt; x.A.B.C</c> chain.
/// </summary>
/// <remarks>
/// The <see cref="PropertyInfo"/> comes straight out of the expression tree, so the trimmer roots the getter
/// (the C# compiler emits an <c>ldtoken</c> for it). Reading goes through reflection rather than a compiled
/// delegate, because <see cref="Expression{TDelegate}.Compile()"/> needs runtime code generation and would
/// break NativeAOT.
/// </remarks>
internal sealed class PropertyLink(PropertyInfo property)
{
    private readonly PropertyInfo _property = property;

    /// <summary>Gets the property name, as it appears in <see cref="PropertyChangedEventArgs"/>.</summary>
    public string Name { get; } = property.Name;

    /// <summary>Reads the property off the given instance.</summary>
    /// <param name="instance">The object to read from.</param>
    /// <returns>The current value.</returns>
    public object? GetValue(object instance) => _property.GetValue(instance);
}

/// <summary>
/// Turns property-access lambdas into observable sequences without compiling the expression tree.
/// </summary>
internal static class PropertyChain
{
    /// <summary>
    /// Parses <c>x =&gt; x.A.B</c> into the ordered links <c>[A, B]</c>.
    /// </summary>
    /// <typeparam name="TSender">The lambda parameter type.</typeparam>
    /// <typeparam name="TValue">The value the lambda produces.</typeparam>
    /// <param name="expression">The property-access lambda.</param>
    /// <param name="parameterName">The name of the caller's argument, used in exception messages.</param>
    /// <returns>The links, outermost first.</returns>
    /// <exception cref="ArgumentException">The lambda is anything other than a chain of property reads.</exception>
    public static PropertyLink[] Parse<TSender, TValue>(
        Expression<Func<TSender, TValue>> expression,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var links = new List<PropertyLink>();
        var node = Unwrap(expression.Body);

        while (node is MemberExpression member)
        {
            if (member.Member is not PropertyInfo property)
            {
                throw new ArgumentException(
                    $"'{expression}' reads the field '{member.Member.Name}'. Only properties raise change " +
                    "notifications, so only property chains can be observed.",
                    parameterName);
            }

            links.Add(new PropertyLink(property));
            node = Unwrap(member.Expression);
        }

        if (node is not ParameterExpression || links.Count == 0)
        {
            throw new ArgumentException(
                $"'{expression}' is not a property chain. Expected something shaped like 'x => x.Foo.Bar'.",
                parameterName);
        }

        links.Reverse();
        return [.. links];
    }

    /// <summary>
    /// Parses a lambda that must read exactly one property, and returns its name.
    /// </summary>
    /// <typeparam name="TSender">The lambda parameter type.</typeparam>
    /// <typeparam name="TValue">The value the lambda produces.</typeparam>
    /// <param name="expression">The property-access lambda.</param>
    /// <param name="parameterName">The name of the caller's argument, used in exception messages.</param>
    /// <returns>The property name.</returns>
    /// <exception cref="ArgumentException">The lambda reads more or less than one property.</exception>
    public static string SingleName<TSender, TValue>(
        Expression<Func<TSender, TValue>> expression,
        string parameterName)
    {
        var links = Parse(expression, parameterName);
        return links.Length == 1
            ? links[0].Name
            : throw new ArgumentException(
                $"'{expression}' walks {links.Length} properties, but a single property was expected here.",
                parameterName);
    }

    /// <summary>
    /// Observes a parsed chain, re-reading it whenever any link along the way changes.
    /// </summary>
    /// <param name="root">The object the chain starts at.</param>
    /// <param name="chain">The links produced by <see cref="Parse{TSender, TValue}"/>.</param>
    /// <param name="beforeChange">
    /// When <see langword="true"/>, the last link reports its value just before it changes rather than just
    /// after. Intermediate links always use change notifications, since the chain has to follow them first.
    /// </param>
    /// <returns>The current value, then every subsequent value. The sequence never completes.</returns>
    public static IObservable<object?> Observe(object root, PropertyLink[] chain, bool beforeChange = false)
    {
        var current = Observable.Return<object?>(root);

        for (var i = 0; i < chain.Length; i++)
        {
            var link = chain[i];
            var linkBeforeChange = beforeChange && i == chain.Length - 1;

            current = current
                .Select(owner => owner is null
                    ? Observable.Return<object?>(null)
                    : ObserveOne(owner, link, linkBeforeChange))
                .Switch();
        }

        return current;
    }

    /// <summary>
    /// Converts a value read off the chain to the type the lambda promised.
    /// </summary>
    /// <typeparam name="TValue">The type the lambda promised.</typeparam>
    /// <param name="value">The value that was read.</param>
    /// <param name="propertyName">The property it came from, for the exception message.</param>
    /// <returns>The typed value.</returns>
    /// <exception cref="InvalidCastException">
    /// The value is of some other type, which means a cast written inside the lambda does not hold at runtime.
    /// </exception>
    /// <remarks>
    /// Null is converted rather than rejected: it is what a chain reports when a link in the middle is null,
    /// and for a value type the only thing an absent value can be is its default. Anything else is a bug, and
    /// is raised rather than quietly replaced with a default that would look like real data.
    /// </remarks>
    public static TValue Convert<TValue>(object? value, string propertyName) => value switch
    {
        TValue typed => typed,
        null => default!,
        _ => throw new InvalidCastException(
            $"Property '{propertyName}' produced a value of type '{value.GetType()}', but the expression "
            + $"promised '{typeof(TValue)}'."),
    };

    /// <summary>
    /// Strips the conversions the compiler inserts when the lambda's return type is wider than the property's.
    /// </summary>
    /// <param name="node">The expression node to unwrap.</param>
    /// <returns>The innermost operand.</returns>
    private static Expression? Unwrap(Expression? node)
    {
        while (node is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
        {
            node = unary.Operand;
        }

        return node;
    }

    /// <summary>
    /// Observes a single property on a single object.
    /// </summary>
    /// <param name="owner">The object holding the property.</param>
    /// <param name="link">The property to read.</param>
    /// <param name="beforeChange">Whether to report the value just before it changes rather than just after.</param>
    /// <returns>
    /// The current value, then every subsequent value. Objects that raise no notifications produce their value
    /// once and are then treated as constant.
    /// </returns>
    private static IObservable<object?> ObserveOne(object owner, PropertyLink link, bool beforeChange) =>
        beforeChange ? ObserveBeforeChange(owner, link) : ObserveAfterChange(owner, link);

    private static IObservable<object?> ObserveAfterChange(object owner, PropertyLink link) =>
        Observable.Create<object?>(observer =>
        {
            if (owner is not INotifyPropertyChanged notifier)
            {
                observer.OnNext(link.GetValue(owner));
                return Disposable.Empty;
            }

            // Subscribe before the first read, so a change racing with subscription is not lost.
            PropertyChangedEventHandler handler = (_, e) =>
            {
                if (Matches(e.PropertyName, link.Name))
                {
                    observer.OnNext(link.GetValue(owner));
                }
            };

            notifier.PropertyChanged += handler;
            observer.OnNext(link.GetValue(owner));

            return Disposable.Create(
                (notifier, handler),
                static state => state.notifier.PropertyChanged -= state.handler);
        });

    private static IObservable<object?> ObserveBeforeChange(object owner, PropertyLink link) =>
        Observable.Create<object?>(observer =>
        {
            if (owner is not INotifyPropertyChanging notifier)
            {
                observer.OnNext(link.GetValue(owner));
                return Disposable.Empty;
            }

            // Reading inside the changing notification is what makes this the *old* value.
            PropertyChangingEventHandler handler = (_, e) =>
            {
                if (Matches(e.PropertyName, link.Name))
                    observer.OnNext(link.GetValue(owner));
            };

            notifier.PropertyChanging += handler;
            observer.OnNext(link.GetValue(owner));

            return Disposable.Create(
                (notifier, handler),
                static state => state.notifier.PropertyChanging -= state.handler);
        });

    /// <summary>
    /// Decides whether a notification is about the property we are watching.
    /// </summary>
    /// <param name="notified">The name carried by the notification.</param>
    /// <param name="watched">The name of the property being watched.</param>
    /// <returns><see langword="true"/> for a match, including the "everything changed" empty name.</returns>
    private static bool Matches(string? notified, string watched) =>
        string.IsNullOrEmpty(notified) || string.Equals(notified, watched, StringComparison.Ordinal);
}