namespace Reactive.Avalonia;

/// <summary>
/// One rule inside a <see cref="ValidationContext"/>.
/// </summary>
public interface IValidationComponent
{
    /// <summary>
    /// Gets the properties this rule reports errors for. Empty for rules that apply to the model as a whole.
    /// </summary>
    IReadOnlyList<string> PropertyNames { get; }

    /// <summary>
    /// Gets the current outcome of the rule.
    /// </summary>
    ValidationState ValidationStatus { get; }

    /// <summary>
    /// Gets a sequence that ticks each time the outcome changes.
    /// </summary>
    IObservable<ValidationState> ValidationStatusChange { get; }
}