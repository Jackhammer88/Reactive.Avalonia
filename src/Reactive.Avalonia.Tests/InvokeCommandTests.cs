using System.Windows.Input;

namespace Reactive.Avalonia.Tests;

/// <summary>
/// Covers <see cref="InvokeCommandMixins"/>.
/// </summary>
/// <remarks>Adapted from the <c>InvokeCommand_*</c> tests in ReactiveUI's <c>ReactiveCommandTest</c>.</remarks>
[TestFixture]
public class InvokeCommandTests : ReactiveTestBase
{
    [Test]
    public void DrivesAReactiveCommand()
    {
        var received = new List<int>();
        using var command = ReactiveCommand.Create<int>(received.Add);
        var source = new Subject<int>();

        using (source.InvokeCommand(command))
        {
            source.OnNext(1);
            source.OnNext(2);
        }

        source.OnNext(3);

        Assert.That(received, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void RespectsCanExecute()
    {
        var received = new List<int>();
        var canExecute = new BehaviorSubject<bool>(false);
        using var command = ReactiveCommand.Create<int>(received.Add, canExecute);
        var source = new Subject<int>();

        using var _ = source.InvokeCommand(command);

        source.OnNext(1);
        Assert.That(received, Is.Empty, "Values arriving while blocked are dropped, not queued.");

        canExecute.OnNext(true);
        source.OnNext(2);

        Assert.That(received, Is.EqualTo(new[] { 2 }));
    }

    [Test]
    public void DoesNotTearDownOnCommandFailure()
    {
        var attempts = 0;
        using var command = ReactiveCommand.Create<int>(_ =>
        {
            attempts++;
            throw new InvalidOperationException("boom");
        });
        command.ThrownExceptions.Subscribe(static _ => { });

        var source = new Subject<int>();
        using var _ = source.InvokeCommand(command);

        source.OnNext(1);
        source.OnNext(2);

        Assert.That(attempts, Is.EqualTo(2), "A failing command must not kill the pipeline feeding it.");
    }

    [Test]
    public void DrivesAPlainICommand()
    {
        var received = new List<object?>();
        var command = new FakeCommand(received);
        var source = new Subject<int>();

        using var _ = source.InvokeCommand((ICommand)command);
        source.OnNext(42);

        Assert.That(received, Is.EqualTo(new object?[] { 42 }));
    }

    [Test]
    public void RespectsCanExecuteOnAPlainICommand()
    {
        var received = new List<object?>();
        var command = new FakeCommand(received) { CanExecuteResult = false };
        var source = new Subject<int>();

        using var _ = source.InvokeCommand((ICommand)command);
        source.OnNext(42);

        Assert.That(received, Is.Empty);
    }

    [Test]
    public void LooksUpTheCommandOnTheTargetEachTime()
    {
        var first = new List<int>();
        var second = new List<int>();
        var host = new CommandHost { Command = ReactiveCommand.Create<int>(first.Add) };
        var source = new Subject<int>();

        using var _ = source.InvokeCommand(host, x => x.Command);

        source.OnNext(1);
        host.Command = ReactiveCommand.Create<int>(second.Add);
        source.OnNext(2);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(new[] { 1 }));
            Assert.That(second, Is.EqualTo(new[] { 2 }), "Reassigning the property redirects later values.");
        });
    }

    [Test]
    public void ToleratesANullCommandProperty()
    {
        var host = new CommandHost();
        var source = new Subject<int>();

        using var _ = source.InvokeCommand(host, x => x.Command);

        Assert.DoesNotThrow(() => source.OnNext(1));
    }

    private sealed class CommandHost : ReactiveObject
    {
        private ICommand? _command;

        public ICommand? Command
        {
            get => _command;
            set => this.RaiseAndSetIfChanged(ref _command, value);
        }
    }

    private sealed class FakeCommand(List<object?> received) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecuteResult { get; init; } = true;

        public bool CanExecute(object? parameter) => CanExecuteResult;

        public void Execute(object? parameter) => received.Add(parameter);

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}