using System.Linq.Expressions;

namespace Reactive.Avalonia;

/// <summary>
/// Declares validation rules on a view model.
/// </summary>
/// <remarks>
/// Rules are live: each one watches the properties it depends on and re-evaluates itself as they change.
/// Failures reach the UI through <see cref="INotifyDataErrorInfo"/>, which Avalonia reads without any extra
/// setup — a <c>TextBox</c> bound to a failing property shows the message on its own.
/// </remarks>
/// <example>
/// <code language="csharp">
/// public sealed class SignUpViewModel : ReactiveValidationObject
/// {
///     public SignUpViewModel()
///     {
///         this.ValidationRule(x => x.Email, email => email.Contains('@'), "Enter a valid email.");
///         this.ValidationRule(
///             x => x.Confirm,
///             this.WhenAnyValue(x => x.Password, x => x.Confirm, (p, c) => p == c),
///             "The passwords do not match.");
///
///         SignUp = ReactiveCommand.CreateFromTask(SignUpAsync, ValidationContext.Valid);
///     }
/// }
/// </code>
/// </example>
public static class ValidationMixins
{
    /// <summary>
    /// Adds a rule that checks one property against a predicate.
    /// </summary>
    /// <typeparam name="TViewModel">The view model type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="viewModel">The view model to add the rule to.</param>
    /// <param name="property">A lambda naming the property, shaped like <c>x =&gt; x.Foo</c>.</param>
    /// <param name="isValid">Returns whether the current value is acceptable.</param>
    /// <param name="message">The message shown while the rule fails.</param>
    /// <returns>A handle to the rule.</returns>
    public static ValidationHelper ValidationRule<TViewModel, TProperty>(
        this TViewModel viewModel,
        Expression<Func<TViewModel, TProperty>> property,
        Func<TProperty, bool> isValid,
        string message)
        where TViewModel : class, IValidatableViewModel
    {
        ArgumentNullException.ThrowIfNull(isValid);
        ArgumentNullException.ThrowIfNull(message);
        return viewModel.ValidationRule(property, isValid, _ => message);
    }

    /// <summary>
    /// Adds a rule that checks one property against a predicate, with a message built from the value.
    /// </summary>
    /// <typeparam name="TViewModel">The view model type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="viewModel">The view model to add the rule to.</param>
    /// <param name="property">A lambda naming the property, shaped like <c>x =&gt; x.Foo</c>.</param>
    /// <param name="isValid">Returns whether the current value is acceptable.</param>
    /// <param name="message">Builds the message shown while the rule fails.</param>
    /// <returns>A handle to the rule.</returns>
    public static ValidationHelper ValidationRule<TViewModel, TProperty>(
        this TViewModel viewModel,
        Expression<Func<TViewModel, TProperty>> property,
        Func<TProperty, bool> isValid,
        Func<TProperty, string> message)
        where TViewModel : class, IValidatableViewModel
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(isValid);
        ArgumentNullException.ThrowIfNull(message);

        var propertyName = PropertyChain.SingleName(property, nameof(property));
        var states = viewModel
            .WhenAnyValue(property)
            .Select(value => isValid(value) ? ValidationState.Valid : ValidationState.Invalid(message(value)));

        return Register(viewModel, states, [propertyName]);
    }

    /// <summary>
    /// Adds a rule for one property whose validity is decided by an observable sequence.
    /// </summary>
    /// <typeparam name="TViewModel">The view model type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="viewModel">The view model to add the rule to.</param>
    /// <param name="property">A lambda naming the property the error is reported against.</param>
    /// <param name="isValid">Ticks <see langword="true"/> while the rule is satisfied.</param>
    /// <param name="message">The message shown while the rule fails.</param>
    /// <returns>A handle to the rule.</returns>
    /// <remarks>
    /// Use this for rules that span several properties — pass a <c>WhenAnyValue</c> chain and report the error
    /// against whichever field the user should fix.
    /// </remarks>
    public static ValidationHelper ValidationRule<TViewModel, TProperty>(
        this TViewModel viewModel,
        Expression<Func<TViewModel, TProperty>> property,
        IObservable<bool> isValid,
        string message)
        where TViewModel : class, IValidatableViewModel
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(isValid);
        ArgumentNullException.ThrowIfNull(message);

        var propertyName = PropertyChain.SingleName(property, nameof(property));
        return Register(viewModel, ToStates(isValid, message), [propertyName]);
    }

    /// <summary>
    /// Adds a rule that applies to the view model as a whole rather than to a single property.
    /// </summary>
    /// <typeparam name="TViewModel">The view model type.</typeparam>
    /// <param name="viewModel">The view model to add the rule to.</param>
    /// <param name="isValid">Ticks <see langword="true"/> while the rule is satisfied.</param>
    /// <param name="message">The message shown while the rule fails.</param>
    /// <returns>A handle to the rule.</returns>
    public static ValidationHelper ValidationRule<TViewModel>(
        this TViewModel viewModel,
        IObservable<bool> isValid,
        string message)
        where TViewModel : class, IValidatableViewModel
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(isValid);
        ArgumentNullException.ThrowIfNull(message);

        return Register(viewModel, ToStates(isValid, message), []);
    }

    /// <summary>
    /// Gets a sequence tracking whether every rule on the view model is satisfied.
    /// </summary>
    /// <typeparam name="TViewModel">The view model type.</typeparam>
    /// <param name="viewModel">The view model to observe.</param>
    /// <returns>The current validity, then one value per change.</returns>
    public static IObservable<bool> IsValid<TViewModel>(this TViewModel viewModel)
        where TViewModel : class, IValidatableViewModel
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        return viewModel.ValidationContext.Valid;
    }

    private static IObservable<ValidationState> ToStates(IObservable<bool> isValid, string message) =>
        isValid.Select(valid => valid ? ValidationState.Valid : ValidationState.Invalid(message));

    private static ValidationHelper Register(
        IValidatableViewModel viewModel,
        IObservable<ValidationState> states,
        IReadOnlyList<string> propertyNames)
    {
        var component = new ObservableValidation(states, propertyNames);
        return new ValidationHelper(component, viewModel.ValidationContext.Add(component));
    }
}