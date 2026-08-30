using Avalonia.Controls;

namespace Reactive.Avalonia.IntegrationTests.Mocks;

/// <summary>
/// A code-only <see cref="ReactiveView{TViewModel}"/> so the view tests need no XAML.
/// </summary>
public sealed class CounterView : ReactiveView<CounterViewModel>
{
    public CounterView()
    {
        NameBox = new TextBox();
        GreetButton = new Button { Content = "Greet" };

        Content = new StackPanel { Children = { NameBox, GreetButton } };

        this.WhenActivated(disposables =>
        {
            Activations++;
            disposables.Add(Disposable.Create(() => Deactivations++));
        });
    }

    public TextBox NameBox { get; }

    public Button GreetButton { get; }

    public int Activations { get; private set; }

    public int Deactivations { get; private set; }
}