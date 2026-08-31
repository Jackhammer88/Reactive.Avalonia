using System.Threading;

namespace Reactive.Avalonia;

/// <summary>
/// The set of validation rules attached to a view model, and their combined outcome.
/// </summary>
/// <remarks>
/// Rules are added with the <see cref="ValidationMixins"/> extension methods rather than by hand. The context
/// raises change notifications for <see cref="IsValid"/> and <see cref="Text"/>, so both can be observed with
/// <c>WhenAnyValue</c> or bound to directly.
/// </remarks>
public sealed class ValidationContext : ReactiveObject, IDisposable
{
    private readonly List<IValidationComponent> _components = [];
    private readonly Dictionary<IValidationComponent, IDisposable> _subscriptions = [];
    private readonly BehaviorSubject<ValidationState> _status = new(ValidationState.Valid);
    private readonly Subject<IReadOnlyList<string>> _changedProperties = new();
    private readonly Lock _gate = new();

    /// <summary>
    /// Gets the combined outcome of every rule, updated as they change.
    /// </summary>
    public IObservable<ValidationState> ValidationStatusChange => _status.AsObservable();

    /// <summary>
    /// Gets a sequence of <see langword="true"/>/<see langword="false"/> as the view model becomes valid or invalid.
    /// </summary>
    /// <remarks>This is what you feed to a command's <c>canExecute</c>.</remarks>
    public IObservable<bool> Valid => _status.Select(static state => state.IsValid).DistinctUntilChanged();

    /// <summary>
    /// Gets the properties affected by the most recent change, so views can raise targeted notifications.
    /// </summary>
    internal IObservable<IReadOnlyList<string>> ChangedProperties => _changedProperties.AsObservable();

    /// <summary>
    /// Gets a value indicating whether every rule is currently satisfied.
    /// </summary>
    public bool IsValid => _status.Value.IsValid;

    /// <summary>
    /// Gets the messages from every rule that is currently failing.
    /// </summary>
    public IReadOnlyList<string> Text => _status.Value.Messages;

    /// <summary>
    /// Registers a rule.
    /// </summary>
    /// <param name="component">The rule to add.</param>
    /// <returns>A token that removes the rule when disposed.</returns>
    public IDisposable Add(IValidationComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);

        lock (_gate)
        {
            _components.Add(component);
            _subscriptions[component] = component.ValidationStatusChange.Subscribe(_ =>
            {
                Recompute();
                _changedProperties.OnNext(component.PropertyNames);
            });
        }

        Recompute();
        _changedProperties.OnNext(component.PropertyNames);

        return Disposable.Create((self: this, component), static state => state.self.Remove(state.component));
    }

    /// <summary>
    /// Removes a rule.
    /// </summary>
    /// <param name="component">The rule to remove.</param>
    public void Remove(IValidationComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);

        lock (_gate)
        {
            if (!_components.Remove(component))
            {
                return;
            }

            if (_subscriptions.Remove(component, out var subscription))
            {
                subscription.Dispose();
            }
        }

        Recompute();
        _changedProperties.OnNext(component.PropertyNames);
    }

    /// <summary>
    /// Gets the failing messages for one property.
    /// </summary>
    /// <param name="propertyName">
    /// The property to look up. Pass <see langword="null"/> or an empty string for every message, whatever
    /// property it came from.
    /// </param>
    /// <returns>The messages to show for that property.</returns>
    public IReadOnlyList<string> GetErrors(string? propertyName)
    {
        List<string>? errors = null;

        lock (_gate)
        {
            foreach (var component in _components)
            {
                var status = component.ValidationStatus;
                if (status.IsValid)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(propertyName) &&
                    !component.PropertyNames.Contains(propertyName, StringComparer.Ordinal))
                {
                    continue;
                }

                (errors ??= []).AddRange(status.Messages);
            }
        }

        return errors ?? (IReadOnlyList<string>)[];
    }

    /// <summary>
    /// Releases every registered rule.
    /// </summary>
    public void Dispose()
    {
        lock (_gate)
        {
            foreach (var subscription in _subscriptions.Values)
            {
                subscription.Dispose();
            }

            foreach (var component in _components)
            {
                (component as IDisposable)?.Dispose();
            }

            _subscriptions.Clear();
            _components.Clear();
        }

        _status.Dispose();
        _changedProperties.Dispose();
    }

    private void Recompute()
    {
        var valid = true;
        List<string>? messages = null;

        lock (_gate)
        {
            foreach (var component in _components)
            {
                var status = component.ValidationStatus;
                if (status.IsValid)
                {
                    continue;
                }

                valid = false;
                (messages ??= []).AddRange(status.Messages);
            }
        }

        var next = new ValidationState(valid, messages ?? (IReadOnlyList<string>)[]);
        var previous = _status.Value;

        // Raised around the push so that an observer woken by ValidationStatusChange already sees the new
        // IsValid and Text, and so that a binding to either of them is not left showing a stale value.
        var validityChanged = previous.IsValid != next.IsValid;
        var textChanged = !previous.Messages.SequenceEqual(next.Messages, StringComparer.Ordinal);

        if (validityChanged)
        {
            RaisePropertyChanging(nameof(IsValid));
        }

        if (textChanged)
        {
            RaisePropertyChanging(nameof(Text));
        }

        _status.OnNext(next);

        if (validityChanged)
        {
            RaisePropertyChanged(nameof(IsValid));
        }

        if (textChanged)
        {
            RaisePropertyChanged(nameof(Text));
        }
    }
}