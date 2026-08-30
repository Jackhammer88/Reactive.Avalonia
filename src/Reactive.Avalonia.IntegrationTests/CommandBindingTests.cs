using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;

using Reactive.Avalonia.IntegrationTests.Mocks;

namespace Reactive.Avalonia.IntegrationTests;

/// <summary>
/// Checks that a <see cref="ReactiveCommand{TParam, TResult}"/> drives a real control through
/// <see cref="System.Windows.Input.ICommand"/>.
/// </summary>
[TestFixture]
public class CommandBindingTests
{
    [AvaloniaTest]
    public void AFailingValidationDisablesTheButton()
    {
        var view = Show(out _);

        Assert.That(view.GreetButton.IsEffectivelyEnabled, Is.False);
    }

    [AvaloniaTest]
    public void SatisfyingValidationEnablesTheButton()
    {
        var view = Show(out var viewModel);

        viewModel.Name = "Alice";
        Dispatcher.UIThread.RunJobs();

        Assert.That(view.GreetButton.IsEffectivelyEnabled, Is.True);
    }

    [AvaloniaTest]
    public void ClickingTheButtonRunsTheCommand()
    {
        var view = Show(out var viewModel);
        viewModel.Name = "Alice";
        Dispatcher.UIThread.RunJobs();

        Click(view.GreetButton);

        Assert.That(viewModel.Greetings, Is.EqualTo(1));
    }

    [AvaloniaTest]
    public void ADisabledCommandDoesNotRunOnClick()
    {
        var view = Show(out var viewModel);

        Click(view.GreetButton);

        Assert.That(viewModel.Greetings, Is.Zero);
    }

    /// <summary>
    /// Clicks a button the way a user would: focus it and press space, which goes through the button's own
    /// input handling rather than skipping straight to the command.
    /// </summary>
    /// <param name="button">The button to click.</param>
    private static void Click(Button button)
    {
        button.Focus();
        Dispatcher.UIThread.RunJobs();

        var window = (Window)TopLevel.GetTopLevel(button)!;
        window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private static CounterView Show(out CounterViewModel viewModel)
    {
        viewModel = new CounterViewModel();
        var view = new CounterView { ViewModel = viewModel };
        view.GreetButton.Bind(Button.CommandProperty, new Binding(nameof(CounterViewModel.Greet)));

        var window = new Window { Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return view;
    }
}