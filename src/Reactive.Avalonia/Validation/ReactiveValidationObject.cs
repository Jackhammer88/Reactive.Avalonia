using System.Collections;

namespace Reactive.Avalonia;

/// <summary>
/// A <see cref="ReactiveObject"/> that reports its validation errors to the UI.
/// </summary>
/// <remarks>
/// Declare rules with the <see cref="ValidationMixins"/> extension methods. Avalonia reads the resulting
/// <see cref="INotifyDataErrorInfo"/> implementation automatically, so a control bound to a failing property
/// shows the message without any XAML beyond the binding itself.
/// </remarks>
/// <example>
/// <code language="csharp">
/// public sealed class PersonViewModel : ReactiveValidationObject
/// {
///     private string _name = string.Empty;
///
///     public PersonViewModel() =>
///         this.ValidationRule(x => x.Name, name => !string.IsNullOrWhiteSpace(name), "Name is required.");
///
///     public string Name
///     {
///         get => _name;
///         set => RaiseAndSetIfChanged(ref _name, value);
///     }
/// }
/// </code>
/// </example>
public class ReactiveValidationObject : ReactiveObject, IValidatableViewModel, INotifyDataErrorInfo, IDisposable
{
    private readonly IDisposable _subscription;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactiveValidationObject"/> class.
    /// </summary>
    protected ReactiveValidationObject() =>
        _subscription = ValidationContext.ChangedProperties.Subscribe(propertyNames =>
        {
            RaisePropertyChanged(nameof(HasErrors));

            if (propertyNames.Count == 0)
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(null));
                return;
            }

            foreach (var propertyName in propertyNames)
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }
        });

    /// <inheritdoc/>
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    /// <inheritdoc/>
    public ValidationContext ValidationContext { get; } = new();

    /// <inheritdoc/>
    public bool HasErrors => !ValidationContext.IsValid;

    /// <inheritdoc/>
    public IEnumerable GetErrors(string? propertyName) => ValidationContext.GetErrors(propertyName);

    /// <summary>
    /// Releases the validation rules attached to this view model.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the validation rules attached to this view model.
    /// </summary>
    /// <param name="disposing">Whether the call comes from <see cref="Dispose()"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || !disposing)
        {
            return;
        }

        _disposed = true;
        _subscription.Dispose();
        ValidationContext.Dispose();
    }
}