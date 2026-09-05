namespace Reactive.Avalonia;

/// <summary>
/// Ties subscriptions to the lifetime of a view or view model.
/// </summary>
/// <remarks>
/// This is the answer to "where do I dispose this subscription?". Everything set up inside a
/// <c>WhenActivated</c> block is disposed when the view leaves the visual tree, and set up again if it returns.
/// </remarks>
public static class ActivationMixins
{
    /// <summary>
    /// Runs <paramref name="block"/> whenever the view appears, disposing what it registers when the view goes
    /// away.
    /// </summary>
    /// <param name="view">The view to track.</param>
    /// <param name="block">
    /// Receives a <see cref="CompositeDisposable"/> to attach subscriptions to, usually with
    /// <c>DisposeWith</c>.
    /// </param>
    /// <returns>A token that stops tracking the view when disposed.</returns>
    /// <remarks>
    /// If the view is an <see cref="IViewFor"/> whose view model is an <see cref="IActivatableViewModel"/>,
    /// that view model is activated too — including when it is swapped out while the view is on screen.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// public MainWindow()
    /// {
    ///     InitializeComponent();
    ///     this.WhenActivated(disposables =>
    ///         ViewModel!.Save.ThrownExceptions
    ///                   .Subscribe(ShowError)
    ///                   .DisposeWith(disposables));
    /// }
    /// </code>
    /// </example>
    public static IDisposable WhenActivated(
        this IActivatableView view,
        Action<CompositeDisposable> block)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(block);

        return view.WhenActivated(() =>
        {
            var disposables = new CompositeDisposable();
            block(disposables);
            return [disposables];
        });
    }

    /// <summary>
    /// Runs <paramref name="block"/> whenever the view appears, disposing what it returns when the view goes
    /// away.
    /// </summary>
    /// <param name="view">The view to track.</param>
    /// <param name="block">Produces the subscriptions that live for as long as the view is shown.</param>
    /// <returns>A token that stops tracking the view when disposed.</returns>
    public static IDisposable WhenActivated(
        this IActivatableView view, Func<IEnumerable<IDisposable>> block)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(block);

        var handle = new SerialDisposable();

        var subscription = ViewActivation.For(view).Subscribe(active =>
        {
            if (!active)
            {
                handle.Disposable = null;
                return;
            }

            var disposables = new CompositeDisposable();

            foreach (var disposable in block())
            {
                disposables.Add(disposable);
            }

            if (view is IViewFor viewFor)
            {
                disposables.Add(ActivateViewModelOf(viewFor));
            }

            handle.Disposable = disposables;
        });

        return new CompositeDisposable(subscription, handle);
    }

    /// <summary>
    /// Runs <paramref name="block"/> whenever the view model is activated by a view.
    /// </summary>
    /// <param name="viewModel">The view model to track.</param>
    /// <param name="block">
    /// Receives a <see cref="CompositeDisposable"/> to attach subscriptions to, usually with
    /// <c>DisposeWith</c>.
    /// </param>
    /// <returns>A token that unregisters the block when disposed.</returns>
    public static IDisposable WhenActivated(
        this IActivatableViewModel viewModel,
        Action<CompositeDisposable> block)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(block);

        return viewModel.WhenActivated(() =>
        {
            var disposables = new CompositeDisposable();
            block(disposables);
            return [disposables];
        });
    }

    /// <summary>
    /// Runs <paramref name="block"/> whenever the view model is activated by a view.
    /// </summary>
    /// <param name="viewModel">The view model to track.</param>
    /// <param name="block">Produces the subscriptions that live for as long as the view model is active.</param>
    /// <returns>A token that unregisters the block when disposed.</returns>
    public static IDisposable WhenActivated(
        this IActivatableViewModel viewModel,
        Func<IEnumerable<IDisposable>> block)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(block);
        return viewModel.Activator.AddActivationBlock(block);
    }

    /// <summary>
    /// Keeps the view's current view model activated, following it if the view model is replaced.
    /// </summary>
    /// <param name="view">The view whose view model should be activated.</param>
    /// <returns>A token that deactivates the view model when disposed.</returns>
    private static IDisposable ActivateViewModelOf(IViewFor view)
    {
        var handle = new SerialDisposable();

        var subscription = ObserveViewModel(view)
            .Subscribe(viewModel =>
                handle.Disposable = viewModel is IActivatableViewModel activatable
                    ? activatable.Activator.Activate()
                    : null);

        return new CompositeDisposable(subscription, handle);
    }

    /// <summary>
    /// Observes the view's <see cref="IViewFor.ViewModel"/> property.
    /// </summary>
    /// <param name="view">The view to observe.</param>
    /// <returns>The current view model, then each replacement.</returns>
    private static IObservable<object?> ObserveViewModel(IViewFor view)
    {
        ArgumentNullException.ThrowIfNull(view);

        return Observable.Create<object?>(observer =>
        {
            if (view is not INotifyPropertyChanged notifier)
            {
                observer.OnNext(view.ViewModel);
                return Disposable.Empty;
            }

            PropertyChangedEventHandler handler = (_, e) =>
            {
                if (string.IsNullOrEmpty(e.PropertyName) ||
                    string.Equals(e.PropertyName, nameof(IViewFor.ViewModel), StringComparison.Ordinal))
                {
                    observer.OnNext(view.ViewModel);
                }
            };

            notifier.PropertyChanged += handler;
            observer.OnNext(view.ViewModel);

            return Disposable.Create(
                (notifier, handler),
                static state => state.notifier.PropertyChanged -= state.handler);
        });
    }
}