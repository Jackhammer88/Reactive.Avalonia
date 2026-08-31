// Derived from ReactiveUI 23.2.28 (ScheduledSubject).
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT license.
// See THIRD-PARTY-NOTICES.md in the repository root.

using System.Threading;

namespace Reactive.Avalonia;

/// <summary>
/// A subject that forwards to a fallback observer whenever nobody else is subscribed.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <remarks>
/// This is what makes an unobserved <see cref="IHandleObservableErrors.ThrownExceptions"/> loud: while no one
/// is listening the values go to <see cref="RxSchedulers.DefaultExceptionHandler"/>, and the moment somebody
/// subscribes the fallback steps aside.
/// </remarks>
internal sealed class ScheduledSubject<T> : ISubject<T>, IDisposable
{
    private readonly IObserver<T>? _fallback;
    private readonly IScheduler _scheduler;
    private readonly Subject<T> _subject = new();
    private IDisposable _fallbackSubscription = Disposable.Empty;
    private int _observerCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledSubject{T}"/> class.
    /// </summary>
    /// <param name="scheduler">The scheduler observers are notified on.</param>
    /// <param name="fallback">The observer used while nothing else is subscribed.</param>
    public ScheduledSubject(IScheduler scheduler, IObserver<T>? fallback = null)
    {
        _scheduler = scheduler;
        _fallback = fallback;

        if (fallback is not null)
        {
            _fallbackSubscription = _subject.ObserveOn(scheduler).Subscribe(fallback);
        }
    }

    /// <inheritdoc/>
    public void OnCompleted() => _subject.OnCompleted();

    /// <inheritdoc/>
    public void OnError(Exception error) => _subject.OnError(error);

    /// <inheritdoc/>
    public void OnNext(T value) => _subject.OnNext(value);

    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<T> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        if (Interlocked.Increment(ref _observerCount) == 1)
        {
            Interlocked.Exchange(ref _fallbackSubscription, Disposable.Empty).Dispose();
        }

        return new CompositeDisposable(
            _subject.ObserveOn(_scheduler).Subscribe(observer),
            Disposable.Create(this, static state => state.ReleaseObserver()));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Interlocked.Exchange(ref _fallbackSubscription, Disposable.Empty).Dispose();
        _subject.Dispose();
    }

    private void ReleaseObserver()
    {
        if (Interlocked.Decrement(ref _observerCount) != 0 || _fallback is null)
        {
            return;
        }

        Interlocked
            .Exchange(ref _fallbackSubscription, _subject.ObserveOn(_scheduler).Subscribe(_fallback))
            .Dispose();
    }
}