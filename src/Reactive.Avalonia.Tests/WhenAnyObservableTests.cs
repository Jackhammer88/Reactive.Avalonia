namespace Reactive.Avalonia.Tests;

/// <summary>
/// Covers <see cref="WhenAnyObservableMixins"/>.
/// </summary>
/// <remarks>Adapted from ReactiveUI's <c>WhenAnyObservableTests</c>.</remarks>
[TestFixture]
public class WhenAnyObservableTests : ReactiveTestBase
{
    [Test]
    public void ForwardsValuesFromTheCurrentSequence()
    {
        var fixture = new ObservableHost();
        var output = new List<int>();

        fixture.WhenAnyObservable(x => x.First!).Subscribe(output.Add);

        fixture.First!.OnNext(1);
        fixture.First.OnNext(2);

        Assert.That(output, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void SwitchesWhenThePropertyIsReplaced()
    {
        var fixture = new ObservableHost();
        var original = fixture.First!;
        var output = new List<int>();

        fixture.WhenAnyObservable(x => x.First!).Subscribe(output.Add);

        original.OnNext(1);
        fixture.First = new Subject<int>();
        fixture.First.OnNext(2);
        original.OnNext(99);

        Assert.That(output, Is.EqualTo(new[] { 1, 2 }), "The replaced sequence is no longer listened to.");
    }

    [Test]
    public void TreatsANullPropertyAsAnEmptySequence()
    {
        var fixture = new ObservableHost { First = null };
        var output = new List<int>();

        Assert.DoesNotThrow(() => fixture.WhenAnyObservable(x => x.First!).Subscribe(output.Add));

        fixture.First = new Subject<int>();
        fixture.First.OnNext(1);

        Assert.That(output, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void MergesSeveralSequences()
    {
        var fixture = new ObservableHost();
        var output = new List<int>();

        fixture.WhenAnyObservable(x => x.First!, x => x.Second!).Subscribe(output.Add);

        fixture.First!.OnNext(1);
        fixture.Second!.OnNext(2);
        fixture.First.OnNext(3);

        Assert.That(output, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void ObservesACommandsExecutionState()
    {
        var host = new CommandHost();
        var states = new List<bool>();

        host.WhenAnyObservable(x => x.Command.IsExecuting).Subscribe(states.Add);

        host.Command.Execute().Subscribe();

        Assert.That(states, Is.EqualTo(new[] { false, true, false }));
    }

    private sealed class ObservableHost : ReactiveObject
    {
        private Subject<int>? _first = new();
        private Subject<int>? _second = new();

        public Subject<int>? First
        {
            get => _first;
            set => this.RaiseAndSetIfChanged(ref _first, value);
        }

        public Subject<int>? Second
        {
            get => _second;
            set => this.RaiseAndSetIfChanged(ref _second, value);
        }
    }

    private sealed class CommandHost : ReactiveObject
    {
        public ReactiveCommand<Unit, Unit> Command { get; } = ReactiveCommand.Create(static () => { });
    }
}