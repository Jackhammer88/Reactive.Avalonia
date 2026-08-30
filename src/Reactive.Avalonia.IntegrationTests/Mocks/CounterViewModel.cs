namespace Reactive.Avalonia.IntegrationTests.Mocks;

/// <summary>
/// A view model that records its activation lifetime, for the view tests to assert against.
/// </summary>
public sealed class CounterViewModel : ReactiveValidationObject, IActivatableViewModel
{
    private string _name = string.Empty;

    public CounterViewModel()
    {
        this.ValidationRule(x => x.Name, static name => !string.IsNullOrWhiteSpace(name), "Name is required.");

        Greet = ReactiveCommand.Create(() => { Greetings++; }, this.IsValid());

        this.WhenActivated(disposables =>
        {
            Activations++;
            disposables.Add(Disposable.Create(() => Deactivations++));
        });
    }

    public ViewModelActivator Activator { get; } = new();

    public int Activations { get; private set; }

    public int Deactivations { get; private set; }

    public int Greetings { get; private set; }

    public ReactiveCommand<Unit, Unit> Greet { get; }

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }
}