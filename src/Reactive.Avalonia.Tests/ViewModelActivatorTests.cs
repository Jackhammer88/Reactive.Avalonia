namespace Reactive.Avalonia.Tests;

/// <summary>
/// Covers <see cref="ViewModelActivator"/> and the view-model half of <see cref="ActivationMixins"/>.
/// </summary>
/// <remarks>Adapted from ReactiveUI's <c>ViewModelActivatorTests</c> and <c>ActivatingViewModelTests</c>.</remarks>
[TestFixture]
public class ViewModelActivatorTests : ReactiveTestBase
{
    [Test]
    public void ActivatingTicksTheActivatedSequence()
    {
        using var activator = new ViewModelActivator();
        var activated = 0;
        activator.Activated.Subscribe(_ => activated++);

        activator.Activate();

        Assert.That(activated, Is.EqualTo(1));
    }

    [Test]
    public void DeactivatingWithoutActivatingTicksNothing()
    {
        using var activator = new ViewModelActivator();
        var deactivated = 0;
        activator.Deactivated.Subscribe(_ => deactivated++);

        activator.Deactivate();

        Assert.That(deactivated, Is.Zero);
    }

    [Test]
    public void DeactivatingAfterActivatingTicksTheDeactivatedSequence()
    {
        using var activator = new ViewModelActivator();
        var deactivated = 0;
        activator.Deactivated.Subscribe(_ => deactivated++);

        activator.Activate();
        activator.Deactivate();

        Assert.That(deactivated, Is.EqualTo(1));
    }

    [Test]
    public void DeactivatingIgnoringTheReferenceCountTicksTheDeactivatedSequence()
    {
        using var activator = new ViewModelActivator();
        var deactivated = 0;
        activator.Deactivated.Subscribe(_ => deactivated++);

        activator.Deactivate(ignoreRefCount: true);

        Assert.That(deactivated, Is.EqualTo(1));
    }

    [Test]
    public void DisposingTheHandleDeactivates()
    {
        using var activator = new ViewModelActivator();
        var activated = 0;
        var deactivated = 0;
        activator.Activated.Subscribe(_ => activated++);
        activator.Deactivated.Subscribe(_ => deactivated++);

        using (activator.Activate())
        {
            Assert.Multiple(() =>
            {
                Assert.That(activated, Is.EqualTo(1));
                Assert.That(deactivated, Is.Zero);
            });
        }

        Assert.Multiple(() =>
        {
            Assert.That(activated, Is.EqualTo(1));
            Assert.That(deactivated, Is.EqualTo(1));
        });
    }

    [Test]
    public void ActivationIsReferenceCounted()
    {
        var viewModel = new ActivatableViewModel();

        var first = viewModel.Activator.Activate();
        var second = viewModel.Activator.Activate();

        Assert.That(viewModel.Runs, Is.EqualTo(1), "Two views showing one view model activate it once.");

        second.Dispose();
        Assert.That(viewModel.Disposals, Is.Zero, "One view is still showing it.");

        first.Dispose();
        Assert.That(viewModel.Disposals, Is.EqualTo(1));
    }

    [Test]
    public void ReactivationRunsTheBlockAgain()
    {
        var viewModel = new ActivatableViewModel();

        viewModel.Activator.Activate().Dispose();
        viewModel.Activator.Activate().Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Runs, Is.EqualTo(2));
            Assert.That(viewModel.Disposals, Is.EqualTo(2));
        });
    }

    [Test]
    public void RegisteringWhileAlreadyActiveRunsTheBlockImmediately()
    {
        var viewModel = new ActivatableViewModel();
        using var handle = viewModel.Activator.Activate();
        var lateRuns = 0;

        viewModel.WhenActivated(_ => lateRuns++);

        Assert.That(lateRuns, Is.EqualTo(1), "A block added mid-activation should not wait for the next one.");
    }

    [Test]
    public void UnregisteringABlockStopsItRunning()
    {
        var viewModel = new ActivatableViewModel();
        var runs = 0;

        var registration = viewModel.WhenActivated(_ => runs++);
        registration.Dispose();

        viewModel.Activator.Activate();

        Assert.That(runs, Is.Zero);
    }

    [Test]
    public void DisposingTheActivatorTearsDownAnyActiveBlocks()
    {
        var viewModel = new ActivatableViewModel();
        viewModel.Activator.Activate();

        viewModel.Activator.Dispose();

        Assert.That(viewModel.Disposals, Is.EqualTo(1));
    }

    private sealed class ActivatableViewModel : ReactiveObject, IActivatableViewModel
    {
        public ActivatableViewModel() =>
            this.WhenActivated(disposables =>
            {
                Runs++;
                disposables.Add(Disposable.Create(() => Disposals++));
            });

        public ViewModelActivator Activator { get; } = new();

        public int Runs { get; private set; }

        public int Disposals { get; private set; }
    }
}