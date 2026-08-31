using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;

using Reactive.Avalonia.Sample.ViewModels;
using Reactive.Avalonia.Sample.Views;

namespace Reactive.Avalonia.IntegrationTests;

/// <summary>
/// Drives the sample application itself, so the XAML, the view models and the library are all exercised
/// together rather than in isolation.
/// </summary>
[TestFixture]
public class SampleAppTests
{
    [AvaloniaTest]
    public void TheMainWindowShowsOneTabPerViewModel()
    {
        using var viewModel = new MainViewModel();
        var window = new MainWindow { DataContext = viewModel };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        var tabs = window.GetVisualDescendants().OfType<TabControl>().Single();

        Assert.That(tabs.ItemCount, Is.EqualTo(viewModel.TabVms.Count));
    }

    [AvaloniaTest]
    public void TheValidationTabDisablesGreetUntilTheNameIsValid()
    {
        using var viewModel = new ValidationViewModel();
        var view = new Validation { ViewModel = viewModel };
        var window = new Window { Content = view };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        var button = view.GetVisualDescendants().OfType<Button>().Single();
        Assert.That(button.IsEffectivelyEnabled, Is.False, "The name starts empty, so the rule fails.");

        var textBox = view.GetVisualDescendants().OfType<TextBox>().Single();
        textBox.Focus();
        window.KeyTextInput("Alice");
        Dispatcher.UIThread.RunJobs();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Name, Is.EqualTo("Alice"));
            Assert.That(button.IsEffectivelyEnabled, Is.True);
            Assert.That(DataValidationErrors.GetHasErrors(textBox), Is.False);
        });
    }

    [AvaloniaTest]
    public void TheValidationTabReportsATooLongName()
    {
        using var viewModel = new ValidationViewModel();
        var view = new Validation { ViewModel = viewModel };
        var window = new Window { Content = view };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        viewModel.Name = "a name well past the limit";
        Dispatcher.UIThread.RunJobs();

        var textBox = view.GetVisualDescendants().OfType<TextBox>().Single();
        var errors = DataValidationErrors.GetErrors(textBox)?.Select(static error => error.ToString()).ToArray();

        Assert.That(errors, Is.EqualTo(new[] { "Name is 26 characters; 10 is the limit." }));
    }

    [AvaloniaTest]
    public void TheCommandTabReportsExecutionState()
    {
        using var viewModel = new CommandViewModel();
        var view = new Command { ViewModel = viewModel };
        var window = new Window { Content = view };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.That(viewModel.JobStatus, Is.EqualTo("Stopped"));

        var execution = viewModel.RunJobCommand.Execute(60).Subscribe(static _ => { }, static _ => { });
        Dispatcher.UIThread.RunJobs();

        Assert.That(viewModel.JobStatus, Is.EqualTo("Executing"));

        execution.Dispose();
        Dispatcher.UIThread.RunJobs();

        Assert.That(viewModel.JobStatus, Is.EqualTo("Stopped"), "Disposing the execution cancels the job.");
    }

    [AvaloniaTest]
    public void TheInteractionTabAsksTheViewForAFile()
    {
        using var viewModel = new InteractionViewModel();
        using var handler = viewModel.OpenFileInteraction.RegisterHandler(
            static context => context.SetOutput("/tmp/chosen.txt"));

        viewModel.SelectFileCommand.Execute().Subscribe();
        Dispatcher.UIThread.RunJobs();

        Assert.That(viewModel.SelectedFile, Is.EqualTo("/tmp/chosen.txt"));
    }

    [AvaloniaTest]
    public void TheInteractionViewRegistersItsHandlerOnlyWhileItIsShown()
    {
        using var viewModel = new InteractionViewModel();
        var view = new Interaction { ViewModel = viewModel };
        var window = new Window { Content = view };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        // The view's own handler answers with null because the headless storage provider has no files.
        string? answer = "unset";
        viewModel.OpenFileInteraction.Handle(Unit.Default).Subscribe(value => answer = value);
        Dispatcher.UIThread.RunJobs();
        Assert.That(answer, Is.Null);

        window.Close();
        Dispatcher.UIThread.RunJobs();

        Exception? caught = null;
        viewModel.OpenFileInteraction.Handle(Unit.Default).Subscribe(static _ => { }, ex => caught = ex);

        Assert.That(
            caught,
            Is.TypeOf<UnhandledInteractionException>(),
            "Closing the window must unregister the handler it installed.");
    }
}