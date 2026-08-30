using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Reactive.Avalonia;

/// <summary>
/// Derives a view's activation state from its place in the Avalonia visual tree.
/// </summary>
internal static class ViewActivation
{
    /// <summary>
    /// Observes whether the view is currently on screen.
    /// </summary>
    /// <param name="view">The view to track.</param>
    /// <returns>
    /// The current state, then one value per transition. Views that are not Avalonia visuals are permanently
    /// inactive.
    /// </returns>
    public static IObservable<bool> For(IActivatableView view) => view switch
    {
        Control control => ForControl(control),
        Visual visual => ForVisual(visual),
        _ => Observable.Return(false),
    };

    private static IObservable<bool> ForControl(Control control) =>
        Observable.Create<bool>(observer =>
        {
            EventHandler<RoutedEventArgs> loaded = (_, _) => observer.OnNext(true);
            EventHandler<RoutedEventArgs> unloaded = (_, _) => observer.OnNext(false);

            control.Loaded += loaded;
            control.Unloaded += unloaded;
            observer.OnNext(control.IsLoaded);

            return Disposable.Create(
                (control, loaded, unloaded),
                static state =>
                {
                    state.control.Loaded -= state.loaded;
                    state.control.Unloaded -= state.unloaded;
                });
        }).DistinctUntilChanged();

    private static IObservable<bool> ForVisual(Visual visual) =>
        Observable.Create<bool>(observer =>
        {
            EventHandler<VisualTreeAttachmentEventArgs> attached = (_, _) => observer.OnNext(true);
            EventHandler<VisualTreeAttachmentEventArgs> detached = (_, _) => observer.OnNext(false);

            visual.AttachedToVisualTree += attached;
            visual.DetachedFromVisualTree += detached;
            observer.OnNext(TopLevel.GetTopLevel(visual) is not null);

            return Disposable.Create(
                (visual, attached, detached),
                static state =>
                {
                    state.visual.AttachedToVisualTree -= state.attached;
                    state.visual.DetachedFromVisualTree -= state.detached;
                });
        }).DistinctUntilChanged();
}