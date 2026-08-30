using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;

using Reactive.Avalonia.IntegrationTests.Mocks;

namespace Reactive.Avalonia.IntegrationTests;

/// <summary>
/// Covers <see cref="ReactiveView{TViewModel}"/> and <see cref="ReactiveWindow{TViewModel}"/> inside a real
/// Avalonia visual tree.
/// </summary>
/// <remarks>Adapted from ReactiveUI's <c>ActivatingViewTests</c>, which cannot run without a UI stack.</remarks>
[TestFixture]
public class ReactiveViewTests
{
    [AvaloniaTest]
    public void SettingTheDataContextSetsTheViewModel()
    {
        var view = new CounterView();
        var viewModel = new CounterViewModel();

        view.DataContext = viewModel;

        Assert.That(view.ViewModel, Is.SameAs(viewModel));
    }

    [AvaloniaTest]
    public void SettingTheViewModelSetsTheDataContext()
    {
        var view = new CounterView();
        var viewModel = new CounterViewModel();

        view.ViewModel = viewModel;

        Assert.That(view.DataContext, Is.SameAs(viewModel));
    }

    [AvaloniaTest]
    public void AnUnrelatedDataContextDoesNotOverwriteTheViewModel()
    {
        var viewModel = new CounterViewModel();
        var view = new CounterView { ViewModel = viewModel };

        view.DataContext = "not a view model";

        Assert.That(view.ViewModel, Is.SameAs(viewModel), "A mistyped DataContext must not silently clear it.");
    }

    [AvaloniaTest]
    public void TheViewModelIsReachableThroughTheNonGenericInterface()
    {
        var viewModel = new CounterViewModel();
        var view = new CounterView();

        ((IViewFor)view).ViewModel = viewModel;

        Assert.Multiple(() =>
        {
            Assert.That(((IViewFor)view).ViewModel, Is.SameAs(viewModel));
            Assert.That(view.ViewModel, Is.SameAs(viewModel));
        });
    }

    [AvaloniaTest]
    public void ShowingTheWindowActivatesTheView()
    {
        var view = new CounterView();
        var window = new Window { Content = view };

        Assert.That(view.Activations, Is.Zero, "Nothing is active before the control reaches the tree.");

        window.Show();
        Pump();

        Assert.That(view.Activations, Is.EqualTo(1));
    }

    [AvaloniaTest]
    public void ClosingTheWindowDeactivatesTheView()
    {
        var view = new CounterView();
        var window = new Window { Content = view };

        window.Show();
        Pump();
        window.Close();
        Pump();

        Assert.Multiple(() =>
        {
            Assert.That(view.Activations, Is.EqualTo(1));
            Assert.That(view.Deactivations, Is.EqualTo(1));
        });
    }

    [AvaloniaTest]
    public void TheViewActivatesItsViewModel()
    {
        var viewModel = new CounterViewModel();
        var view = new CounterView { ViewModel = viewModel };
        var window = new Window { Content = view };

        window.Show();
        Pump();
        Assert.That(viewModel.Activations, Is.EqualTo(1));

        window.Close();
        Pump();
        Assert.That(viewModel.Deactivations, Is.EqualTo(1));
    }

    [AvaloniaTest]
    public void SwappingTheViewModelWhileShownMovesActivationAcross()
    {
        var first = new CounterViewModel();
        var second = new CounterViewModel();
        var view = new CounterView { ViewModel = first };
        var window = new Window { Content = view };

        window.Show();
        Pump();
        view.ViewModel = second;
        Pump();

        Assert.Multiple(() =>
        {
            Assert.That(first.Deactivations, Is.EqualTo(1), "The outgoing view model is deactivated.");
            Assert.That(second.Activations, Is.EqualTo(1), "The incoming one is activated in its place.");
        });
    }

    [AvaloniaTest]
    public void ReattachingTheViewActivatesItAgain()
    {
        var view = new CounterView();
        var host = new ContentControl { Content = view };
        var window = new Window { Content = host };

        window.Show();
        Pump();
        host.Content = null;
        Pump();
        host.Content = view;
        Pump();

        Assert.Multiple(() =>
        {
            Assert.That(view.Activations, Is.EqualTo(2));
            Assert.That(view.Deactivations, Is.EqualTo(1));
        });
    }

    [AvaloniaTest]
    public void AReactiveWindowActivatesItsViewModel()
    {
        var viewModel = new CounterViewModel();
        var window = new CounterWindow { ViewModel = viewModel };

        window.Show();
        Pump();
        Assert.That(viewModel.Activations, Is.EqualTo(1));

        window.Close();
        Pump();
        Assert.That(viewModel.Deactivations, Is.EqualTo(1));
    }

    /// <summary>
    /// Drains the dispatcher so queued lifecycle events such as <see cref="Control.Loaded"/> are delivered.
    /// </summary>
    private static void Pump() => Dispatcher.UIThread.RunJobs();

    private sealed class CounterWindow : ReactiveWindow<CounterViewModel>;
}