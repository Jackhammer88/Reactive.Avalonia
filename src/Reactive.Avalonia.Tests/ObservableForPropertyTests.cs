using Reactive.Avalonia.Tests.Mocks;

namespace Reactive.Avalonia.Tests;

/// <summary>
/// Covers <see cref="ObservableForPropertyMixins"/>.
/// </summary>
/// <remarks>Adapted from the <c>OFP*</c> tests in ReactiveUI's <c>ReactiveNotifyPropertyChangedMixinTest</c>.</remarks>
[TestFixture]
public class ObservableForPropertyTests : ReactiveTestBase
{
    [Test]
    public void SkipsTheCurrentValueByDefault()
    {
        var fixture = new TestFixture { IsOnlyOneWord = "Foo" };
        var output = new List<IObservedChange<TestFixture, string?>>();

        fixture.ObservableForProperty(x => x.IsOnlyOneWord).Subscribe(output.Add);
        Assert.That(output, Is.Empty);

        fixture.IsOnlyOneWord = "Bar";
        Assert.That(output, Has.Count.EqualTo(1));
    }

    [Test]
    public void ReportsSenderNameAndValue()
    {
        var fixture = new TestFixture { IsNotNullString = "Foo" };
        var output = new List<IObservedChange<TestFixture, string?>>();

        fixture.ObservableForProperty(x => x.IsNotNullString).Subscribe(output.Add);
        fixture.IsNotNullString = "Bar";

        Assert.Multiple(() =>
        {
            Assert.That(output[0].Sender, Is.SameAs(fixture));
            Assert.That(output[0].PropertyName, Is.EqualTo(nameof(TestFixture.IsNotNullString)));
            Assert.That(output[0].Value, Is.EqualTo("Bar"));
        });
    }

    [Test]
    public void CanEmitTheCurrentValue()
    {
        var fixture = new TestFixture { IsOnlyOneWord = "Foo" };
        var output = new List<string?>();

        fixture.ObservableForProperty(x => x.IsOnlyOneWord, skipInitial: false)
               .Subscribe(change => output.Add(change.Value));

        fixture.IsOnlyOneWord = "Bar";

        Assert.That(output, Is.EqualTo(new[] { "Foo", "Bar" }));
    }

    [Test]
    public void ReportsTheOutgoingValueWhenAskedForBeforeChange()
    {
        var fixture = new TestFixture { IsOnlyOneWord = "Foo" };
        var output = new List<string?>();

        fixture.ObservableForProperty(x => x.IsOnlyOneWord, beforeChange: true)
               .Subscribe(change => output.Add(change.Value));

        fixture.IsOnlyOneWord = "Bar";
        fixture.IsOnlyOneWord = "Baz";

        Assert.That(output, Is.EqualTo(new[] { "Foo", "Bar" }), "Each notification carries the value being replaced.");
    }

    [Test]
    public void DropsRepeatedValuesByDefault()
    {
        var fixture = new InpcOnlyFixture();
        var count = 0;

        fixture.ObservableForProperty(x => x.Property).Subscribe(_ => count++);

        fixture.Property = "Foo";

        // The fixture reports "everything changed" without the value actually moving.
        fixture.RaiseWholeObjectChanged();

        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void CanBeToldToKeepRepeatedValues()
    {
        var fixture = new InpcOnlyFixture();
        var count = 0;

        fixture.ObservableForProperty(x => x.Property, isDistinct: false).Subscribe(_ => count++);

        fixture.Property = "Foo";
        fixture.RaiseWholeObjectChanged();

        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public void FollowsAChainThroughAReplacedLink()
    {
        var host = new HostTestFixture { Child = new TestFixture { IsOnlyOneWord = "Foo" } };
        var output = new List<string?>();

        host.ObservableForProperty(x => x.Child!.IsOnlyOneWord).Subscribe(change => output.Add(change.Value));

        host.Child!.IsOnlyOneWord = "Bar";
        host.Child = new TestFixture { IsOnlyOneWord = "Baz" };

        Assert.That(output, Is.EqualTo(new[] { "Bar", "Baz" }));
    }

    [Test]
    public void ProjectsThroughASelector()
    {
        var fixture = new TestFixture { IsOnlyOneWord = "Foo" };
        var output = new List<int>();

        fixture.ObservableForProperty(x => x.IsOnlyOneWord, static value => value!.Length).Subscribe(output.Add);

        fixture.IsOnlyOneWord = "Bumbershoot";

        Assert.That(output, Is.EqualTo(new[] { 11 }));
    }

    [Test]
    public void RejectsBeforeChangeOnATypeThatCannotReportIt()
    {
        // The constraint can demand INotifyPropertyChanged but not also INotifyPropertyChanging, so this case
        // has to fail at the call rather than hand back a sequence that never ticks.
        var fixture = new ChangedOnlyFixture();

        var exception = Assert.Throws<ArgumentException>(() =>
            fixture.ObservableForProperty(x => x.Value, beforeChange: true).Subscribe());

        Assert.That(exception!.Message, Does.Contain("INotifyPropertyChanging"));
    }

    [Test]
    public void RejectsANullSelector()
    {
        var fixture = new TestFixture();

        Assert.Throws<ArgumentNullException>(() =>
            fixture.ObservableForProperty(x => x.IsOnlyOneWord, (Func<string?, int>)null!).Subscribe());
    }

    [Test]
    public void RejectsANullSender()
    {
        TestFixture? fixture = null;

        Assert.Throws<ArgumentNullException>(() =>
            fixture!.ObservableForProperty(x => x.IsOnlyOneWord).Subscribe());
    }
}