using System.Reactive;

namespace Reactive.Avalonia.Sample.ViewModels;

public class ValidationViewModel : TabItemViewModelBase
{
    public ValidationViewModel()
    {
        this.ValidationRule(
            vm => vm.Name,
            name => !string.IsNullOrWhiteSpace(name),
            "Name is required.");

        this.ValidationRule(
            vm => vm.Name,
            name => name.Length <= 10,
            name => $"Name is {name.Length} characters; 10 is the limit.");

        Greet = ReactiveCommand.Create(
            () => { LastGreeting = $"Hi, {Name}!"; },
            this.IsValid());
    }

    /// <summary>Gets or sets the name to greet. Validated on every keystroke.</summary>
    public string Name
    {
        get;
        set => RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    /// <summary>Gets the greeting produced by the last successful <see cref="Greet"/>.</summary>
    public string LastGreeting
    {
        get;
        private set => RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    /// <summary>Gets the command that greets <see cref="Name"/>.
    /// Disabled while validation fails.</summary>
    public ReactiveCommand<Unit, Unit> Greet { get; }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Greet.Dispose();
        }

        base.Dispose(disposing);
    }

    public override string Title => "Validation";
}