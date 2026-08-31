namespace Reactive.Avalonia;

/// <summary>
/// A handle to one registered validation rule.
/// </summary>
/// <remarks>
/// Keep it in a field when you want to read the rule's state from the UI (for example, to show a hint next to a
/// single field) or to remove the rule later. Otherwise the return value can be ignored — the rule stays
/// registered for the lifetime of the <see cref="ValidationContext"/>.
/// </remarks>
public sealed class ValidationHelper : ReactiveObject, IDisposable
{
    private readonly ObservableValidation _component;
    private readonly IDisposable _registration;
    private readonly IDisposable _notifications;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationHelper"/> class.
    /// </summary>
    /// <param name="component">The rule.</param>
    /// <param name="registration">The token that removes the rule from its context.</param>
    internal ValidationHelper(ObservableValidation component, IDisposable registration)
    {
        _component = component;
        _registration = registration;

        // Without these, binding to Rule.Message would show the first message and then never move.
        _notifications = component.ValidationStatusChange.Subscribe(_ =>
        {
            RaisePropertyChanged(nameof(IsValid));
            RaisePropertyChanged(nameof(Message));
        });
    }

    /// <summary>
    /// Gets a value indicating whether the rule is currently satisfied.
    /// </summary>
    public bool IsValid => _component.ValidationStatus.IsValid;

    /// <summary>
    /// Gets the message for the current failure, or an empty string when the rule is satisfied.
    /// </summary>
    public string Message =>
        _component.ValidationStatus.Messages.Count > 0 ? _component.ValidationStatus.Messages[0] : string.Empty;

    /// <summary>
    /// Gets a sequence that ticks each time the rule's outcome changes.
    /// </summary>
    public IObservable<ValidationState> ValidationChanged => _component.ValidationStatusChange;

    /// <summary>
    /// Removes the rule from its context.
    /// </summary>
    public void Dispose()
    {
        _notifications.Dispose();
        _registration.Dispose();
        _component.Dispose();
    }
}