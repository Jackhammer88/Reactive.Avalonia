namespace Reactive.Avalonia;

/// <summary>
/// A rule whose outcome is driven by an observable sequence.
/// </summary>
internal sealed class ObservableValidation : IValidationComponent, IDisposable
{
    private readonly ReplaySubject<ValidationState> _changes = new(1);
    private readonly IDisposable _subscription;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableValidation"/> class.
    /// </summary>
    /// <param name="source">Produces the rule's outcome.</param>
    /// <param name="propertyNames">The properties this rule reports errors for.</param>
    public ObservableValidation(IObservable<ValidationState> source, IReadOnlyList<string> propertyNames)
    {
        PropertyNames = propertyNames;
        _subscription = source.Subscribe(state =>
        {
            ValidationStatus = state;
            _changes.OnNext(state);
        });
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> PropertyNames { get; }

    /// <inheritdoc/>
    public ValidationState ValidationStatus { get; private set; } = ValidationState.Valid;

    /// <inheritdoc/>
    public IObservable<ValidationState> ValidationStatusChange => _changes.AsObservable();

    /// <inheritdoc/>
    public void Dispose()
    {
        _subscription.Dispose();
        _changes.Dispose();
    }
}