using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;

using Reactive.Avalonia.IntegrationTests.Mocks;

namespace Reactive.Avalonia.IntegrationTests;

/// <summary>
/// Checks that the validation rules declared on a view model actually reach the controls bound to them.
/// </summary>
/// <remarks>
/// This is the part that cannot be unit tested: it depends on Avalonia reading
/// <see cref="System.ComponentModel.INotifyDataErrorInfo"/> off the data context on its own.
/// </remarks>
[TestFixture]
public class ValidationBindingTests
{
    [AvaloniaTest]
    public void AFailingRuleMarksTheBoundControl()
    {
        var viewModel = new CounterViewModel();
        var view = new CounterView { ViewModel = viewModel };
        view.NameBox.Bind(TextBox.TextProperty, new Binding(nameof(CounterViewModel.Name)));

        var window = new Window { Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.That(DataValidationErrors.GetHasErrors(view.NameBox), Is.True);
    }

    [AvaloniaTest]
    public void TheErrorMessageReachesTheControl()
    {
        var viewModel = new CounterViewModel();
        var view = new CounterView { ViewModel = viewModel };
        view.NameBox.Bind(TextBox.TextProperty, new Binding(nameof(CounterViewModel.Name)));

        var window = new Window { Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var errors = DataValidationErrors.GetErrors(view.NameBox)?.Select(static error => error.ToString()).ToArray();

        Assert.That(errors, Is.EqualTo(new[] { "Name is required." }));
    }

    [AvaloniaTest]
    public void FixingTheValueClearsTheControl()
    {
        var viewModel = new CounterViewModel();
        var view = new CounterView { ViewModel = viewModel };
        view.NameBox.Bind(TextBox.TextProperty, new Binding(nameof(CounterViewModel.Name)));

        var window = new Window { Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        viewModel.Name = "Alice";
        Dispatcher.UIThread.RunJobs();

        Assert.That(DataValidationErrors.GetHasErrors(view.NameBox), Is.False);
    }

    [AvaloniaTest]
    public void TypingIntoTheControlRevalidates()
    {
        var viewModel = new CounterViewModel();
        var view = new CounterView { ViewModel = viewModel };
        view.NameBox.Bind(TextBox.TextProperty, new Binding(nameof(CounterViewModel.Name)));

        var window = new Window { Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        view.NameBox.Focus();
        window.KeyTextInput("Alice");
        Dispatcher.UIThread.RunJobs();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Name, Is.EqualTo("Alice"));
            Assert.That(DataValidationErrors.GetHasErrors(view.NameBox), Is.False);
        });
    }
}