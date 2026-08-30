namespace Reactive.Avalonia;

/// <summary>
/// The outcome of one validation rule, or of a whole <see cref="ValidationContext"/>.
/// </summary>
/// <param name="IsValid">Whether the rule is currently satisfied.</param>
/// <param name="Messages">The messages to show. Empty when <paramref name="IsValid"/> is <see langword="true"/>.</param>
public sealed record ValidationState(bool IsValid, IReadOnlyList<string> Messages)
{
    /// <summary>
    /// The state of a rule that is satisfied.
    /// </summary>
    public static readonly ValidationState Valid = new(true, []);

    /// <summary>
    /// Creates a failing state carrying a single message.
    /// </summary>
    /// <param name="message">The message to show.</param>
    /// <returns>An invalid state.</returns>
    public static ValidationState Invalid(string message) => new(false, [message]);
}