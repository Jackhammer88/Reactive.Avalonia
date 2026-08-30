namespace Reactive.Avalonia;

/// <summary>
/// Turns an observable into a read-only property that raises change notifications.
/// </summary>
/// <typeparam name="T">The property type.</typeparam>
/// <remarks>
/// Create one with <see cref="OAPHMixins"/>.<c>ToProperty</c> and expose <see cref="Value"/> from the
/// property getter. The source is subscribed immediately, so <see cref="Value"/> is correct before anything
/// reads it. Repeated identical values are dropped rather than raising a redundant notification.
/// </remarks>
public sealed class ObservableAsPropertyHelper<T> : IHandleObservableErrors, IDisposable
{
    private readonly IDisposable _subscription;
    private readonly ScheduledSubject<Exception> _thrownExceptions;
    private T _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableAsPropertyHelper{T}"/> class.
    /// </summary>
    /// <param name="source">The sequence driving the property.</param>
    /// <param name="onChanging">Raises the changing notification, called before <see cref="Value"/> is updated.</param>
    /// <param name="onChanged">Raises the changed notification, called after <see cref="Value"/> is updated.</param>
    /// <param name="initialValue">The value exposed until <paramref name="source"/> produces one.</param>
    /// <param name="scheduler">The scheduler that notifications are raised on.</param>
    internal ObservableAsPropertyHelper(
        IObservable<T> source,
        Action onChanging,
        Action onChanged,
        T initialValue,
        IScheduler scheduler)
    {
        _value = initialValue;
        _thrownExceptions = new ScheduledSubject<Exception>(scheduler, RxSchedulers.DefaultExceptionHandler);

        _subscription = source
            .ObserveOn(scheduler)
            .Subscribe(
                value =>
                {
                    if (EqualityComparer<T>.Default.Equals(_value, value))
                    {
                        return;
                    }

                    onChanging();
                    _value = value;
                    onChanged();
                },
                _thrownExceptions.OnNext);
    }

    /// <summary>
    /// Gets the latest value produced by the source sequence.
    /// </summary>
    public T Value => _value;

    /// <inheritdoc/>
    public IObservable<Exception> ThrownExceptions => _thrownExceptions;

    /// <summary>
    /// Unsubscribes from the source sequence. <see cref="Value"/> keeps its last value.
    /// </summary>
    public void Dispose()
    {
        _subscription.Dispose();
        _thrownExceptions.Dispose();
    }
}