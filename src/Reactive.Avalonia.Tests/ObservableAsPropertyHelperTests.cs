using Reactive.Avalonia.Tests.Mocks;

namespace Reactive.Avalonia.Tests;

/// <summary>
/// Covers <see cref="ObservableAsPropertyHelper{T}"/> and the <c>ToProperty</c> overloads.
/// </summary>
/// <remarks>Adapted from ReactiveUI's <c>ObservableAsPropertyHelperTest</c>.</remarks>
[TestFixture]
public class ObservableAsPropertyHelperTests : ReactiveTestBase
{
    [Test]
    public void ReportsTheInitialValueBeforeTheSourceProducesAnything()
    {
        var fixture = new OaphFixture(new Subject<int>());

        Assert.That(fixture.Value, Is.EqualTo(-5));
    }

    [Test]
    public void ReportsTheLatestValue()
    {
        var input = new Subject<int>();
        var fixture = new OaphFixture(input);

        foreach (var value in new[] { 1, 2, 3, 4 })
        {
            input.OnNext(value);
        }

        Assert.That(fixture.Value, Is.EqualTo(4));

        input.OnCompleted();
        Assert.That(fixture.Value, Is.EqualTo(4), "Completion leaves the last value in place.");
    }

    [Test]
    public void SubscribesToTheSourceImmediately()
    {
        var subscribed = false;
        var source = Observable.Defer(() =>
        {
            subscribed = true;
            return Observable.Return(1);
        });

        _ = new OaphFixture(source);

        Assert.That(subscribed, Is.True, "Value must be correct before anyone reads it.");
    }

    [Test]
    public void RaisesChangingAndChangedForTheHostProperty()
    {
        var input = new Subject<int>();
        var fixture = new OaphFixture(input);
        var changing = new List<string?>();
        var changed = new List<string?>();

        fixture.Changing.Subscribe(args => changing.Add(args.PropertyName));
        fixture.Changed.Subscribe(args => changed.Add(args.PropertyName));

        input.OnNext(7);

        Assert.Multiple(() =>
        {
            Assert.That(changing, Is.EqualTo(new[] { nameof(OaphFixture.Value) }));
            Assert.That(changed, Is.EqualTo(new[] { nameof(OaphFixture.Value) }));
        });
    }

    [Test]
    public void SuppressesDuplicateValues()
    {
        var input = new Subject<int>();
        var fixture = new OaphFixture(input);
        var notifications = new List<int>();

        fixture.Changed.Subscribe(_ => notifications.Add(fixture.Value));

        foreach (var value in new[] { 1, 2, 3, 3, 4 })
        {
            input.OnNext(value);
        }

        Assert.That(notifications, Is.EqualTo(new[] { 1, 2, 3, 4 }), "The repeated 3 raises nothing.");
    }

    [Test]
    public void SkipsTheFirstValueWhenItMatchesTheInitialValue()
    {
        var input = new Subject<int>();
        var fixture = new OaphFixture(input, initialValue: 1);
        var notifications = 0;

        fixture.Changed.Subscribe(_ => notifications++);

        input.OnNext(1);
        Assert.That(notifications, Is.Zero);

        input.OnNext(2);
        Assert.That(notifications, Is.EqualTo(1));
    }

    [Test]
    public void RoutesSourceErrorsToThrownExceptions()
    {
        var input = new Subject<int>();
        var fixture = new OaphFixture(input);
        var errors = new List<Exception>();

        input.OnNext(4);
        fixture.Helper.ThrownExceptions.Subscribe(errors.Add);

        input.OnError(new InvalidOperationException("Die!"));

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Value, Is.EqualTo(4), "The last good value survives the failure.");
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0].Message, Is.EqualTo("Die!"));
        });
    }

    [Test]
    public void AnUnobservedSourceErrorBringsDownTheApplication()
    {
        // Nothing subscribes to ThrownExceptions, so the failure must not be swallowed.
        var input = new Subject<int>();
        var fixture = new OaphFixture(input);
        input.OnNext(4);

        var exception = Assert.Throws<UnhandledErrorException>(() => input.OnError(new Exception("Die!")));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.InnerException?.Message, Is.EqualTo("Die!"));
            Assert.That(fixture.Value, Is.EqualTo(4));
        });
    }

    [Test]
    public void DisposingStopsTrackingButKeepsTheLastValue()
    {
        var input = new Subject<int>();
        var fixture = new OaphFixture(input);

        input.OnNext(1);
        fixture.Helper.Dispose();
        input.OnNext(2);

        Assert.That(fixture.Value, Is.EqualTo(1));
    }

    [Test]
    public void MarshalsOntoTheSuppliedScheduler()
    {
        var scheduler = new Microsoft.Reactive.Testing.TestScheduler();
        var input = new Subject<int>();
        var fixture = new OaphFixture(input, scheduler: scheduler);

        input.OnNext(42);
        Assert.That(fixture.Value, Is.EqualTo(-5), "Nothing has been pumped yet.");

        scheduler.Start();
        Assert.That(fixture.Value, Is.EqualTo(42));
    }

    [Test]
    public void ToPropertyAcceptsAPropertyNameInsteadOfALambda()
    {
        var input = new Subject<int>();
        var owner = new NameOfFixture(input);

        input.OnNext(3);

        Assert.That(owner.Value, Is.EqualTo(3));
    }

    [Test]
    public void ToPropertyRejectsAChainedExpression()
    {
        var host = new HostTestFixture { Child = new TestFixture() };

        var exception = Assert.Throws<ArgumentException>(() =>
            Observable.Return("x").ToProperty(host, static x => x.Child!.IsNotNullString));

        Assert.That(exception!.Message, Does.Contain("single property"));
    }

    private sealed class NameOfFixture : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<int> _value;

        public NameOfFixture(IObservable<int> source) =>
            source.ToProperty(this, nameof(Value), out _value);

        public int Value => _value.Value;
    }
}