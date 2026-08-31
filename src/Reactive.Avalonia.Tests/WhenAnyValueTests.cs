using Reactive.Avalonia.Tests.Mocks;

namespace Reactive.Avalonia.Tests;

/// <summary>
/// Covers <see cref="WhenAnyMixins"/> and the property-chain machinery behind it.
/// </summary>
/// <remarks>Adapted from ReactiveUI's <c>ReactiveNotifyPropertyChangedMixinTest</c>.</remarks>
[TestFixture]
public class WhenAnyValueTests : ReactiveTestBase
{
    [Test]
    public void EmitsTheCurrentValueOnSubscription()
    {
        var fixture = new TestFixture { IsOnlyOneWord = "Foo" };
        var output = new List<string?>();

        fixture.WhenAnyValue(x => x.IsOnlyOneWord).Subscribe(output.Add);

        Assert.That(output, Is.EqualTo(new[] { "Foo" }));
    }

    [Test]
    public void EmitsOnEveryChange()
    {
        var fixture = new TestFixture();
        var output = new List<string?>();

        fixture.WhenAnyValue(x => x.IsOnlyOneWord).Subscribe(output.Add);

        fixture.IsOnlyOneWord = "Foo";
        fixture.IsOnlyOneWord = "Bar";

        Assert.That(output, Is.EqualTo(new[] { null, "Foo", "Bar" }));
    }

    [Test]
    public void StopsEmittingOnceUnsubscribed()
    {
        var fixture = new TestFixture();
        var output = new List<string?>();

        using (fixture.WhenAnyValue(x => x.IsOnlyOneWord).Subscribe(output.Add))
        {
            fixture.IsOnlyOneWord = "Foo";
        }

        fixture.IsOnlyOneWord = "Bar";

        Assert.That(output, Is.EqualTo(new[] { null, "Foo" }));
    }

    [Test]
    public void FollowsAChainThroughAReplacedLink()
    {
        var host = new HostTestFixture { Child = new TestFixture { IsOnlyOneWord = "Foo" } };
        var output = new List<string?>();

        host.WhenAnyValue(x => x.Child!.IsOnlyOneWord).Subscribe(output.Add);

        host.Child!.IsOnlyOneWord = "Bar";
        host.Child = new TestFixture { IsOnlyOneWord = "Baz" };
        host.Child.IsOnlyOneWord = "Bamf";

        Assert.That(output, Is.EqualTo(new[] { "Foo", "Bar", "Baz", "Bamf" }));
    }

    [Test]
    public void StopsWatchingALinkOnceItIsDetached()
    {
        var detached = new TestFixture { IsOnlyOneWord = "Foo" };
        var host = new HostTestFixture { Child = detached };
        var output = new List<string?>();

        host.WhenAnyValue(x => x.Child!.IsOnlyOneWord).Subscribe(output.Add);
        host.Child = new TestFixture { IsOnlyOneWord = "Baz" };

        detached.IsOnlyOneWord = "ignored";

        Assert.That(output, Is.EqualTo(new[] { "Foo", "Baz" }));
    }

    [Test]
    public void EmitsNullWhenTheChainRunsThroughANullLink()
    {
        var host = new HostTestFixture();
        var output = new List<string?>();

        host.WhenAnyValue(x => x.Child!.IsOnlyOneWord).Subscribe(output.Add);
        Assert.That(output, Is.EqualTo(new string?[] { null }));

        host.Child = new TestFixture { IsOnlyOneWord = "Foo" };
        host.Child = null;
        host.Child = new TestFixture { IsOnlyOneWord = "Bar" };

        Assert.That(output, Is.EqualTo(new[] { null, "Foo", null, "Bar" }));
    }

    [Test]
    public void WorksWithAPlainNotifyPropertyChangedObject()
    {
        var fixture = new InpcOnlyFixture { Property = "Foo" };
        var output = new List<string?>();

        fixture.WhenAnyValue(x => x.Property).Subscribe(output.Add);
        fixture.Property = "Bar";

        Assert.That(output, Is.EqualTo(new[] { "Foo", "Bar" }));
    }

    [Test]
    public void TreatsAnEmptyPropertyNameAsEverythingChanged()
    {
        var fixture = new InpcOnlyFixture();
        var output = new List<string?>();

        fixture.WhenAnyValue(x => x.Property).Subscribe(output.Add);

        fixture.SetPropertySilently("Foo");
        Assert.That(output, Has.Count.EqualTo(1), "A silent change is invisible.");

        fixture.RaiseWholeObjectChanged();
        Assert.That(output, Is.EqualTo(new[] { null, "Foo" }));
    }

    [Test]
    public void EmitsOnceForAnObjectThatNotifiesNothing()
    {
        var host = new HostTestFixture { PocoChild = new NonObservableTestFixture { IsNotNullString = "Foo" } };
        var output = new List<string?>();

        host.WhenAnyValue(x => x.PocoChild!.IsNotNullString).Subscribe(output.Add);
        host.PocoChild!.IsNotNullString = "Bar";

        Assert.That(output, Is.EqualTo(new[] { "Foo" }), "A POCO leaf cannot report changes.");
    }

    [Test]
    public void PicksUpAPocoLeafAgainWhenItsOwnerIsReplaced()
    {
        var host = new HostTestFixture { PocoChild = new NonObservableTestFixture { IsNotNullString = "Foo" } };
        var output = new List<string?>();

        host.WhenAnyValue(x => x.PocoChild!.IsNotNullString).Subscribe(output.Add);
        host.PocoChild = new NonObservableTestFixture { IsNotNullString = "Bar" };

        Assert.That(output, Is.EqualTo(new[] { "Foo", "Bar" }));
    }

    [Test]
    public void CombinesTwoPropertiesWithASelector()
    {
        var fixture = new TestFixture { IsNotNullString = "Foo", IsOnlyOneWord = "Bar" };
        var output = new List<string>();

        fixture.WhenAnyValue(x => x.IsNotNullString, x => x.IsOnlyOneWord, (a, b) => $"{a}-{b}")
               .Subscribe(output.Add);

        fixture.IsOnlyOneWord = "Baz";
        fixture.IsNotNullString = "Bamf";

        Assert.That(output, Is.EqualTo(new[] { "Foo-Bar", "Foo-Baz", "Bamf-Baz" }));
    }

    [Test]
    public void CombinesPropertiesIntoATuple()
    {
        var fixture = new TestFixture { IsNotNullString = "Foo", NullableInt = 1 };
        var output = new List<(string?, int?)>();

        fixture.WhenAnyValue(x => x.IsNotNullString, x => x.NullableInt).Subscribe(output.Add);

        fixture.NullableInt = 2;

        Assert.That(output, Is.EqualTo(new (string?, int?)[] { ("Foo", 1), ("Foo", 2) }));
    }

    [Test]
    public void CombinesUpToEightProperties()
    {
        var fixture = new TestFixture { NotNullableInt = 1 };
        var results = new List<int>();

        fixture.WhenAnyValue(
                   x => x.NotNullableInt,
                   x => x.NotNullableInt,
                   x => x.NotNullableInt,
                   x => x.NotNullableInt,
                   x => x.NotNullableInt,
                   x => x.NotNullableInt,
                   x => x.NotNullableInt,
                   x => x.NotNullableInt,
                   (a, b, c, d, e, f, g, h) => a + b + c + d + e + f + g + h)
               .Subscribe(results.Add);

        Assert.That(results[^1], Is.EqualTo(8));
    }

    [Test]
    public void HandlesNonNullableValueTypesWithoutCeremony()
    {
        var fixture = new TestFixture { NotNullableInt = 3 };
        var output = new List<int>();

        fixture.WhenAnyValue(x => x.NotNullableInt).Subscribe(output.Add);
        fixture.NotNullableInt = 4;

        Assert.That(output, Is.EqualTo(new[] { 3, 4 }));
    }

    [Test]
    public void WhenAnyPropertyChangedFiresForAnyProperty()
    {
        var fixture = new TestFixture();
        var count = 0;

        fixture.WhenAnyPropertyChanged().Subscribe(_ => count++);

        Assert.That(count, Is.Zero, "Nothing is emitted on subscription.");

        fixture.IsNotNullString = "Foo";
        fixture.NullableInt = 1;

        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public void WhenAnyPropertyChangedCanBeNarrowedToNamedProperties()
    {
        var fixture = new TestFixture();
        var count = 0;

        fixture.WhenAnyPropertyChanged(nameof(TestFixture.NullableInt)).Subscribe(_ => count++);

        fixture.IsNotNullString = "Foo";
        fixture.NullableInt = 1;

        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void RejectsAnExpressionThatIsNotAPropertyChain()
    {
        var fixture = new TestFixture();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => fixture.WhenAnyValue(static _ => "constant").Subscribe());
            Assert.Throws<ArgumentException>(() => fixture.WhenAnyValue(static x => x.NullableInt == 1).Subscribe());
        });
    }

    [Test]
    public void RaisesWhenALambdaCastDoesNotHold()
    {
        // A cast written inside the lambda is stripped when the chain is parsed, so it is only tested at
        // runtime. Reporting a default here would look exactly like a real value.
        var fixture = new BoxedFixture { Boxed = 42 };
        Exception? caught = null;

        fixture.WhenAnyValue(x => (string)x.Boxed!).Subscribe(static _ => { }, ex => caught = ex);

        Assert.Multiple(() =>
        {
            Assert.That(caught, Is.TypeOf<InvalidCastException>());
            Assert.That(caught!.Message, Does.Contain(nameof(BoxedFixture.Boxed)));
        });
    }

    [Test]
    public void TreatsANullLinkAsTheDefaultValue()
    {
        // Null is the one conversion that stays quiet: it is what a chain reports when a link is missing.
        var host = new HostTestFixture();
        int? seen = -1;

        host.WhenAnyValue(x => x.Child!.NullableInt).Subscribe(value => seen = value);

        Assert.That(seen, Is.Null);
    }

    [Test]
    public void RejectsANullSender()
    {
        TestFixture? fixture = null;

        Assert.Throws<ArgumentNullException>(() => fixture!.WhenAnyValue(x => x.IsOnlyOneWord).Subscribe());
    }

    [Test]
    public void FeedsToPropertyDirectly()
    {
        var fixture = new WhenAnyToPropertyFixture();

        fixture.Source = "Foo";

        Assert.That(fixture.Derived, Is.EqualTo("FOO"));
    }

    private sealed class BoxedFixture : ReactiveObject
    {
        private object? _boxed;

        public object? Boxed
        {
            get => _boxed;
            set => this.RaiseAndSetIfChanged(ref _boxed, value);
        }
    }

    private sealed class WhenAnyToPropertyFixture : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<string> _derived;
        private string _source = string.Empty;

        public WhenAnyToPropertyFixture() =>
            this.WhenAnyValue(x => x.Source)
                .Select(static value => value.ToUpperInvariant())
                .ToProperty(this, x => x.Derived, out _derived);

        public string Source
        {
            get => _source;
            set => this.RaiseAndSetIfChanged(ref _source, value);
        }

        public string Derived => _derived.Value;
    }
}