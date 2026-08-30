using System.Threading;

namespace Reactive.Avalonia;

/// <summary>
/// The base class for view models: raises <see cref="INotifyPropertyChanged"/> and
/// <see cref="INotifyPropertyChanging"/> notifications, and exposes them as observable sequences.
/// </summary>
/// <example>
/// <code language="csharp">
/// public sealed class PersonViewModel : ReactiveObject
/// {
///     private string _name = string.Empty;
///
///     public string Name
///     {
///         get => _name;
///         set => RaiseAndSetIfChanged(ref _name, value);
///     }
/// }
/// </code>
/// </example>
public class ReactiveObject : IReactiveObject
{
    private readonly Lock _gate = new();
    private readonly List<(bool IsChanging, string? PropertyName)> _delayedNotifications = [];
    private Subject<PropertyChangedEventArgs>? _changed;
    private Subject<PropertyChangingEventArgs>? _changing;
    private int _suppressionCount;
    private int _delayCount;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc/>
    public event PropertyChangingEventHandler? PropertyChanging;

    /// <inheritdoc/>
    public IObservable<PropertyChangedEventArgs> Changed =>
        LazyInitializer.EnsureInitialized(ref _changed, static () => new Subject<PropertyChangedEventArgs>());

    /// <inheritdoc/>
    public IObservable<PropertyChangingEventArgs> Changing =>
        LazyInitializer.EnsureInitialized(ref _changing, static () => new Subject<PropertyChangingEventArgs>());

    /// <summary>
    /// Suppresses change notifications until the returned token is disposed.
    /// </summary>
    /// <returns>A token that re-enables notifications when disposed. Suppressions nest.</returns>
    /// <remarks>
    /// Changes made while suppressed are never replayed. Raise the notifications you care about by hand
    /// after disposing the token.
    /// </remarks>
    public IDisposable SuppressChangeNotifications()
    {
        Interlocked.Increment(ref _suppressionCount);
        return Disposable.Create(this, static state => Interlocked.Decrement(ref state._suppressionCount));
    }

    /// <summary>
    /// Queues change notifications until the returned token is disposed, then raises one per property.
    /// </summary>
    /// <returns>A token that flushes the queue when disposed. Delays nest; the flush happens on the last one.</returns>
    /// <remarks>
    /// Use this around a batch update. Repeated changes to the same property collapse into a single
    /// notification carrying the final value, so the UI redraws once instead of once per assignment. Unlike
    /// <see cref="SuppressChangeNotifications"/>, nothing is lost.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// using (viewModel.DelayChangeNotifications())
    /// {
    ///     foreach (var row in rows)
    ///     {
    ///         viewModel.Total += row.Amount;
    ///     }
    /// }
    /// </code>
    /// </example>
    public IDisposable DelayChangeNotifications()
    {
        lock (_gate)
        {
            _delayCount++;
        }

        return Disposable.Create(this, static state => state.EndDelay());
    }

    /// <summary>
    /// Gets a value indicating whether change notifications are currently being raised.
    /// </summary>
    /// <returns><see langword="true"/> unless a <see cref="SuppressChangeNotifications"/> token is outstanding.</returns>
    public bool AreChangeNotificationsEnabled() => Volatile.Read(ref _suppressionCount) == 0;

    /// <inheritdoc/>
    public void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (!AreChangeNotificationsEnabled())
        {
            return;
        }

        if (QueueIfDelayed(isChanging: false, propertyName))
        {
            return;
        }

        RaiseChangedCore(propertyName);
    }

    /// <inheritdoc/>
    public void RaisePropertyChanging([CallerMemberName] string? propertyName = null)
    {
        if (!AreChangeNotificationsEnabled())
        {
            return;
        }

        if (QueueIfDelayed(isChanging: true, propertyName))
        {
            return;
        }

        RaiseChangingCore(propertyName);
    }

    /// <summary>
    /// Assigns <paramref name="newValue"/> to <paramref name="backingField"/> and raises change notifications,
    /// but only when the value actually differs.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    /// <param name="backingField">The backing field to assign to.</param>
    /// <param name="newValue">The new value.</param>
    /// <param name="propertyName">The property name. Supplied by the compiler when omitted.</param>
    /// <returns>The value now held by the property.</returns>
    protected T RaiseAndSetIfChanged<T>(
        ref T backingField,
        T newValue,
        [CallerMemberName] string? propertyName = null)
    {
        ArgumentNullException.ThrowIfNull(propertyName);

        if (EqualityComparer<T>.Default.Equals(backingField, newValue))
        {
            return newValue;
        }

        RaisePropertyChanging(propertyName);
        backingField = newValue;
        RaisePropertyChanged(propertyName);
        return newValue;
    }

    private void RaiseChangedCore(string? propertyName)
    {
        var args = new PropertyChangedEventArgs(propertyName);
        PropertyChanged?.Invoke(this, args);
        _changed?.OnNext(args);
    }

    private void RaiseChangingCore(string? propertyName)
    {
        var args = new PropertyChangingEventArgs(propertyName);
        PropertyChanging?.Invoke(this, args);
        _changing?.OnNext(args);
    }

    /// <summary>
    /// Records a notification for later if notifications are currently delayed.
    /// </summary>
    /// <param name="isChanging">Whether this is a changing rather than a changed notification.</param>
    /// <param name="propertyName">The property the notification is for.</param>
    /// <returns><see langword="true"/> when the notification was queued and must not be raised now.</returns>
    private bool QueueIfDelayed(bool isChanging, string? propertyName)
    {
        lock (_gate)
        {
            if (_delayCount == 0)
            {
                return false;
            }

            // Only the first occurrence is kept: subscribers read the current value, so replaying a property
            // twice would deliver the same value twice.
            if (!_delayedNotifications.Contains((isChanging, propertyName)))
            {
                _delayedNotifications.Add((isChanging, propertyName));
            }

            return true;
        }
    }

    private void EndDelay()
    {
        (bool IsChanging, string? PropertyName)[] queued;

        lock (_gate)
        {
            if (_delayCount == 0 || --_delayCount > 0)
            {
                return;
            }

            queued = [.. _delayedNotifications];
            _delayedNotifications.Clear();
        }

        foreach (var (isChanging, propertyName) in queued)
        {
            if (isChanging)
            {
                RaiseChangingCore(propertyName);
            }
            else
            {
                RaiseChangedCore(propertyName);
            }
        }
    }
}