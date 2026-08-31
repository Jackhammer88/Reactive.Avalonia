using System.Reactive.Disposables.Fluent;
using System.Windows.Input;

using Microsoft.Reactive.Testing;

namespace Reactive.Avalonia.Tests;

/// <summary>
/// Covers <see cref="ReactiveCommand"/> and <see cref="ReactiveCommand{TParam, TResult}"/>.
/// </summary>
/// <remarks>Adapted from ReactiveUI's <c>ReactiveCommandTest</c>.</remarks>
[TestFixture]
public class ReactiveCommandTests : ReactiveTestBase
{
    [Test]
    public void CreateRunsTheAction()
    {
        var executed = 0;
        using var command = ReactiveCommand.Create(() => executed++);

        command.Execute().Subscribe();

        Assert.That(executed, Is.EqualTo(1));
    }

    [Test]
    public void ExecuteIsLazy()
    {
        var executed = 0;
        using var command = ReactiveCommand.Create(() => executed++);

        var execution = command.Execute();
        Assert.That(executed, Is.Zero, "The work starts on subscription, so that disposal can cancel it.");

        execution.Subscribe();
        Assert.That(executed, Is.EqualTo(1));
    }

    [Test]
    public void DisposingAnExecutionCancelsIt()
    {
        var gate = new Subject<Unit>();
        var unsubscribed = false;
        using var command = ReactiveCommand.CreateFromObservable(
            () => gate.Take(1).Finally(() => unsubscribed = true));

        var subscription = command.Execute().Subscribe(static _ => { }, static _ => { });
        subscription.Dispose();

        Assert.That(unsubscribed, Is.True);
    }

    [Test]
    public void ExecuteRunsWithABareSubscribe()
    {
        // Kicking a command off and tying the handle to a lifetime is an ordinary thing to write, so the
        // parameterless Subscribe has to work — and the handle has to remain a cancellation handle.
        var executed = 0;
        using var disposables = new CompositeDisposable();
        using var command = ReactiveCommand.Create(() => executed++);

        command.Execute().Subscribe().DisposeWith(disposables);

        Assert.That(executed, Is.EqualTo(1));
    }

    [Test]
    public void ExecutePassesTheParameter()
    {
        var seen = new List<int>();
        using var command = ReactiveCommand.Create<int>(seen.Add);

        command.Execute(1).Subscribe();
        command.Execute(42).Subscribe();

        Assert.That(seen, Is.EqualTo(new[] { 1, 42 }));
    }

    [Test]
    public void ExecuteDeliversTheResult()
    {
        using var command = ReactiveCommand.Create<int, string>(static value => $"got {value}");
        string? result = null;

        command.Execute(7).Subscribe(value => result = value);

        Assert.That(result, Is.EqualTo("got 7"));
    }

    [Test]
    public void ResultsAreAlsoDeliveredToSubscribersOfTheCommandItself()
    {
        using var command = ReactiveCommand.Create<int, int>(static value => value * 2);
        var results = new List<int>();

        using (command.Subscribe(results.Add))
        {
            command.Execute(2).Subscribe();
            command.Execute(3).Subscribe();
        }

        Assert.That(results, Is.EqualTo(new[] { 4, 6 }));
    }

    [Test]
    public void CanExecuteIsTrueByDefault()
    {
        using var command = ReactiveCommand.Create(static () => { });
        var values = new List<bool>();

        command.CanExecute.Subscribe(values.Add);

        Assert.That(values, Is.EqualTo(new[] { true }));
    }

    [Test]
    public void CanExecuteRespectsTheSuppliedObservable()
    {
        var canExecute = new BehaviorSubject<bool>(false);
        using var command = ReactiveCommand.Create(static () => { }, canExecute);
        var values = new List<bool>();

        command.CanExecute.Subscribe(values.Add);
        canExecute.OnNext(true);
        canExecute.OnNext(false);

        Assert.That(values, Is.EqualTo(new[] { false, true, false }));
    }

    [Test]
    public void CanExecuteOnlyTicksDistinctValues()
    {
        var canExecute = new BehaviorSubject<bool>(false);
        using var command = ReactiveCommand.Create(static () => { }, canExecute);
        var values = new List<bool>();

        command.CanExecute.Subscribe(values.Add);
        canExecute.OnNext(false);
        canExecute.OnNext(true);
        canExecute.OnNext(true);

        Assert.That(values, Is.EqualTo(new[] { false, true }));
    }

    [Test]
    public void CanExecuteIsBehavioural()
    {
        using var command = ReactiveCommand.Create(static () => { });

        // A late subscriber still sees the current value rather than waiting for the next change.
        var values = new List<bool>();
        command.CanExecute.Subscribe(_ => { });
        command.CanExecute.Subscribe(values.Add);

        Assert.That(values, Is.EqualTo(new[] { true }));
    }

    [Test]
    public void CanExecuteIsFalseWhileExecuting()
    {
        var gate = new Subject<Unit>();
        using var command = ReactiveCommand.CreateFromObservable(() => gate.Take(1));
        var values = new List<bool>();

        command.CanExecute.Subscribe(values.Add);
        command.Execute().Subscribe(static _ => { }, static _ => { });
        gate.OnNext(Unit.Default);

        Assert.That(values, Is.EqualTo(new[] { true, false, true }));
    }

    [Test]
    public void CanExecuteFailuresGoToThrownExceptions()
    {
        var canExecute = new Subject<bool>();
        using var command = ReactiveCommand.Create(static () => { }, canExecute);
        var errors = new List<Exception>();

        command.ThrownExceptions.Subscribe(errors.Add);
        command.CanExecute.Subscribe(static _ => { });

        canExecute.OnError(new InvalidOperationException("broken gate"));

        Assert.Multiple(() =>
        {
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0].Message, Is.EqualTo("broken gate"));
        });
    }

    [Test]
    public void IsExecutingTicksAroundTheExecution()
    {
        var gate = new Subject<Unit>();
        using var command = ReactiveCommand.CreateFromObservable(() => gate.Take(1));
        var values = new List<bool>();

        command.IsExecuting.Subscribe(values.Add);
        command.Execute().Subscribe(static _ => { }, static _ => { });

        Assert.That(values, Is.EqualTo(new[] { false, true }));

        gate.OnNext(Unit.Default);
        Assert.That(values, Is.EqualTo(new[] { false, true, false }));
    }

    [Test]
    public void IsExecutingReturnsToFalseAfterAFailure()
    {
        using var command = ReactiveCommand.CreateFromObservable(
            static () => Observable.Throw<Unit>(new InvalidOperationException("boom")));
        var values = new List<bool>();

        command.IsExecuting.Subscribe(values.Add);
        command.ThrownExceptions.Subscribe(static _ => { });
        command.Execute().Subscribe(static _ => { }, static _ => { });

        Assert.That(values, Is.EqualTo(new[] { false, true, false }));
    }

    [Test]
    public void CreateFromTaskAwaitsTheTask()
    {
        using var command = ReactiveCommand.CreateFromTask(static async () =>
        {
            await Task.Yield();
            return 42;
        });

        var result = command.Execute().Wait();

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void CreateFromTaskPassesTheParameter()
    {
        using var command = ReactiveCommand.CreateFromTask<int, int>(static value => Task.FromResult(value * 2));

        Assert.That(command.Execute(21).Wait(), Is.EqualTo(42));
    }

    [Test]
    public void CreateFromTaskSuppliesACancellationToken()
    {
        var started = new TaskCompletionSource();
        var cancelled = false;

        using var command = ReactiveCommand.CreateFromTask(async token =>
        {
            started.TrySetResult();
            try
            {
                await Task.Delay(Timeout.Infinite, token);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
                throw;
            }
        });

        var subscription = command.Execute().Subscribe(static _ => { }, static _ => { });
        Assert.That(started.Task.Wait(TimeSpan.FromSeconds(5)), Is.True);

        subscription.Dispose();

        Assert.That(SpinUntil(() => cancelled), Is.True, "Disposing the subscription cancels the token.");
    }

    [Test]
    public void ThrownExceptionsCapturesLambdaFailures()
    {
        using var command = ReactiveCommand.Create(static () => throw new InvalidOperationException("boom"));
        var errors = new List<Exception>();

        command.ThrownExceptions.Subscribe(errors.Add);
        command.Execute().Subscribe(static _ => { }, static _ => { });

        Assert.Multiple(() =>
        {
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0].Message, Is.EqualTo("boom"));
        });
    }

    [Test]
    public void ExecuteAlsoSurfacesTheFailureToItsSubscribers()
    {
        using var command = ReactiveCommand.CreateFromObservable(
            static () => Observable.Throw<Unit>(new InvalidOperationException("boom")));
        command.ThrownExceptions.Subscribe(static _ => { });

        Exception? caught = null;
        command.Execute().Subscribe(static _ => { }, ex => caught = ex);

        Assert.That(caught, Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void AnUnobservedCommandFailureIsNotSwallowed()
    {
        // Nothing is subscribed to ThrownExceptions, so the failure reaches the default handler, which
        // rethrows rather than letting a broken command look like a no-op.
        using var command = ReactiveCommand.Create(static () => throw new InvalidOperationException("boom"));

        var exception = Assert.Throws<UnhandledErrorException>(() =>
            command.Execute().Subscribe(static _ => { }, static _ => { }));

        Assert.That(exception!.InnerException?.Message, Is.EqualTo("boom"));
    }

    [Test]
    public void CommandsStayUsableAfterAFailure()
    {
        var shouldFail = true;
        using var command = ReactiveCommand.Create(() =>
        {
            if (shouldFail)
            {
                throw new InvalidOperationException("boom");
            }
        });

        command.ThrownExceptions.Subscribe(static _ => { });
        command.Execute().Subscribe(static _ => { }, static _ => { });

        shouldFail = false;
        var canExecute = false;
        command.CanExecute.Subscribe(value => canExecute = value);

        Assert.That(canExecute, Is.True);
    }

    [Test]
    public void ICommandCanExecuteReflectsTheObservable()
    {
        var canExecute = new BehaviorSubject<bool>(false);
        using var command = ReactiveCommand.Create(static () => { }, canExecute);
        var asCommand = (ICommand)command;

        Assert.That(asCommand.CanExecute(null), Is.False);

        canExecute.OnNext(true);
        Assert.That(asCommand.CanExecute(null), Is.True);
    }

    [Test]
    public void ICommandRaisesCanExecuteChanged()
    {
        var canExecute = new BehaviorSubject<bool>(false);
        using var command = ReactiveCommand.Create(static () => { }, canExecute);
        var raised = 0;

        ((ICommand)command).CanExecuteChanged += (_, _) => raised++;
        canExecute.OnNext(true);
        canExecute.OnNext(false);

        Assert.That(raised, Is.EqualTo(2));
    }

    [Test]
    public void ICommandExecuteRunsTheCommand()
    {
        var executed = 0;
        using var command = ReactiveCommand.Create(() => executed++);

        ((ICommand)command).Execute(null);

        Assert.That(executed, Is.EqualTo(1));
    }

    [Test]
    public void ICommandExecuteIgnoresAParameterOnAParameterlessCommand()
    {
        // XAML happily supplies a CommandParameter to a command that takes none; that must not throw.
        var executed = 0;
        using var command = ReactiveCommand.Create(() => executed++);

        Assert.DoesNotThrow(() => ((ICommand)command).Execute("ignored"));
        Assert.That(executed, Is.EqualTo(1));
    }

    [Test]
    public void ICommandExecuteRejectsTheWrongParameterType()
    {
        using var command = ReactiveCommand.Create<int>(static _ => { });

        Assert.Throws<InvalidOperationException>(() => ((ICommand)command).Execute("not an int"));
    }

    [Test]
    public void ResultsAreDeliveredOnTheOutputScheduler()
    {
        var scheduler = new TestScheduler();
        using var command = ReactiveCommand.Create(static () => 1, outputScheduler: scheduler);
        var results = new List<int>();

        command.Execute().Subscribe(results.Add);
        Assert.That(results, Is.Empty);

        scheduler.Start();
        Assert.That(results, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void RejectsANullExecuteBody()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(static () => ReactiveCommand.Create((Action)null!));
            Assert.Throws<ArgumentNullException>(static () => ReactiveCommand.Create((Action<int>)null!));
            Assert.Throws<ArgumentNullException>(static () => ReactiveCommand.Create((Func<int>)null!));
            Assert.Throws<ArgumentNullException>(static () => ReactiveCommand.CreateFromTask((Func<Task>)null!));
            Assert.Throws<ArgumentNullException>(
                static () => ReactiveCommand.CreateFromObservable((Func<IObservable<int>>)null!));
        });
    }

    [Test]
    public void ExecuteAfterDisposalThrows()
    {
        var command = ReactiveCommand.Create(static () => { });
        command.Dispose();

        Assert.Throws<ObjectDisposedException>(() => command.Execute());
    }

    private static bool SpinUntil(Func<bool> condition) =>
        System.Threading.SpinWait.SpinUntil(condition, TimeSpan.FromSeconds(5));
}