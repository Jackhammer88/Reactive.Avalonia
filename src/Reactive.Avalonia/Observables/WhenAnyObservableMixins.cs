using System.Linq.Expressions;

namespace Reactive.Avalonia;

/// <summary>
/// Observes properties that are themselves observable sequences.
/// </summary>
/// <remarks>
/// The point is that the property can be replaced: the subscription follows the current value rather than
/// pinning the sequence that happened to be there when you subscribed. This is what makes
/// <c>this.WhenAnyObservable(x => x.Save.IsExecuting)</c> keep working after <c>Save</c> is reassigned.
/// </remarks>
public static class WhenAnyObservableMixins
{
    /// <summary>
    /// Subscribes to whichever sequence the property currently holds.
    /// </summary>
    /// <typeparam name="TSender">The type being observed.</typeparam>
    /// <typeparam name="TRet">The element type of the inner sequence.</typeparam>
    /// <param name="sender">The object to observe.</param>
    /// <param name="observable1">A lambda selecting the observable-valued property.</param>
    /// <returns>The values of the current inner sequence, switching whenever the property changes.</returns>
    public static IObservable<TRet> WhenAnyObservable<TSender, TRet>(
        this TSender sender,
        Expression<Func<TSender, IObservable<TRet>>> observable1)
        where TSender : class =>
        sender.WhenAnyValue(observable1).Select(Coalesce).Switch();

    /// <summary>
    /// Merges whichever sequences the two properties currently hold.
    /// </summary>
    /// <typeparam name="TSender">The type being observed.</typeparam>
    /// <typeparam name="TRet">The element type of the inner sequences.</typeparam>
    /// <param name="sender">The object to observe.</param>
    /// <param name="observable1">A lambda selecting the first observable-valued property.</param>
    /// <param name="observable2">A lambda selecting the second observable-valued property.</param>
    /// <returns>The merged values of the current inner sequences.</returns>
    public static IObservable<TRet> WhenAnyObservable<TSender, TRet>(
        this TSender sender,
        Expression<Func<TSender, IObservable<TRet>>> observable1,
        Expression<Func<TSender, IObservable<TRet>>> observable2)
        where TSender : class =>
        Observable.Merge(
            sender.WhenAnyObservable(observable1),
            sender.WhenAnyObservable(observable2));

    /// <summary>
    /// Merges whichever sequences the three properties currently hold.
    /// </summary>
    /// <typeparam name="TSender">The type being observed.</typeparam>
    /// <typeparam name="TRet">The element type of the inner sequences.</typeparam>
    /// <param name="sender">The object to observe.</param>
    /// <param name="observable1">A lambda selecting the first observable-valued property.</param>
    /// <param name="observable2">A lambda selecting the second observable-valued property.</param>
    /// <param name="observable3">A lambda selecting the third observable-valued property.</param>
    /// <returns>The merged values of the current inner sequences.</returns>
    public static IObservable<TRet> WhenAnyObservable<TSender, TRet>(
        this TSender sender,
        Expression<Func<TSender, IObservable<TRet>>> observable1,
        Expression<Func<TSender, IObservable<TRet>>> observable2,
        Expression<Func<TSender, IObservable<TRet>>> observable3)
        where TSender : class =>
        Observable.Merge(
            sender.WhenAnyObservable(observable1),
            sender.WhenAnyObservable(observable2),
            sender.WhenAnyObservable(observable3));

    /// <summary>
    /// Treats a property that is currently null as an empty sequence rather than blowing up.
    /// </summary>
    /// <typeparam name="TRet">The element type.</typeparam>
    /// <param name="observable">The sequence held by the property, possibly null.</param>
    /// <returns>The sequence, or an empty one.</returns>
    private static IObservable<TRet> Coalesce<TRet>(IObservable<TRet>? observable) =>
        observable ?? Observable.Empty<TRet>();
}