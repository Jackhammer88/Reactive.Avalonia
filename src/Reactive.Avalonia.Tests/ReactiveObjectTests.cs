using System.ComponentModel;

using Reactive.Avalonia.Tests.Mocks;

namespace Reactive.Avalonia.Tests;

/// <summary>
/// Covers <see cref="ReactiveObject"/> and <see cref="ReactiveObjectExtensions"/>.
/// </summary>
/// <remarks>Adapted from ReactiveUI's <c>ReactiveObjectTests</c>.</remarks>
[TestFixture]
public class ReactiveObjectTests : ReactiveTestBase
{
    [Test]
    public void ChangingAlwaysArrivesBeforeChanged()
    {
        const string beforeSet = "Foo";
        const string afterSet = "Bar";

        var fixture = new TestFixture { IsOnlyOneWord = beforeSet };

        string? changingPropertyName = null;
        string? changingValue = null;
        fixture.Changing.Subscribe(args =>
        {
            changingPropertyName = args.PropertyName;
            changingValue = fixture.IsOnlyOneWord;
        });

        string? changedPropertyName = null;
        string? changedValue = null;
        fixture.Changed.Subscribe(args =>
        {
            changedPropertyName = args.PropertyName;
            changedValue = fixture.IsOnlyOneWord;
        });

        fixture.IsOnlyOneWord = afterSet;

        Assert.Multiple(() =>
        {
            Assert.That(changingPropertyName, Is.EqualTo(nameof(TestFixture.IsOnlyOneWord)));
            Assert.That(changingValue, Is.EqualTo(beforeSet), "Changing must still see the old value.");
            Assert.That(changedPropertyName, Is.EqualTo(nameof(TestFixture.IsOnlyOneWord)));
            Assert.That(changedValue, Is.EqualTo(afterSet));
        });
    }

    [Test]
    public void SmokeTestRaisesChangingAndChangedInStep()
    {
        var fixture = new TestFixture();
        var changing = new List<string?>();
        var changed = new List<string?>();

        fixture.Changing.Subscribe(args => changing.Add(args.PropertyName));
        fixture.Changed.Subscribe(args => changed.Add(args.PropertyName));

        fixture.IsNotNullString = "Foo Bar Baz";
        fixture.IsOnlyOneWord = "Foo";
        fixture.IsOnlyOneWord = "Bar";
        fixture.IsNotNullString = null;
        fixture.IsNotNullString = null;

        string?[] expected =
        [
            nameof(TestFixture.IsNotNullString),
            nameof(TestFixture.IsOnlyOneWord),
            nameof(TestFixture.IsOnlyOneWord),
            nameof(TestFixture.IsNotNullString),
        ];

        Assert.Multiple(() =>
        {
            Assert.That(changed, Is.EqualTo(expected));
            Assert.That(changing, Is.EqualTo(expected));
        });
    }

    [Test]
    public void RaiseAndSetIfChangedIgnoresEqualValues()
    {
        var fixture = new TestFixture { IsNotNullString = "Foo" };
        var raised = new List<string?>();
        fixture.Changed.Subscribe(args => raised.Add(args.PropertyName));

        fixture.UsesExprRaiseSet = "Foo";
        fixture.UsesExprRaiseSet = "Foo";

        Assert.Multiple(() =>
        {
            Assert.That(fixture.UsesExprRaiseSet, Is.EqualTo("Foo"));
            Assert.That(raised, Is.EqualTo(new[] { nameof(TestFixture.UsesExprRaiseSet) }));
        });
    }

    [Test]
    public void RaiseAndSetIfChangedWorksOnHandRolledImplementations()
    {
        var fixture = new HandRolledReactiveObject();
        var changing = new List<string?>();
        var changed = new List<string?>();

        fixture.Changing.Subscribe(args => changing.Add(args.PropertyName));
        fixture.Changed.Subscribe(args => changed.Add(args.PropertyName));

        fixture.Name = "set";
        fixture.Name = "set";

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Name, Is.EqualTo("set"));
            Assert.That(changing, Is.EqualTo(new[] { nameof(HandRolledReactiveObject.Name) }));
            Assert.That(changed, Is.EqualTo(new[] { nameof(HandRolledReactiveObject.Name) }));
        });
    }

    [Test]
    public void SuppressChangeNotificationsDropsNotifications()
    {
        var fixture = new TestFixture();
        var changed = new List<string?>();
        fixture.Changed.Subscribe(args => changed.Add(args.PropertyName));

        using (fixture.SuppressChangeNotifications())
        {
            Assert.That(fixture.AreChangeNotificationsEnabled(), Is.False);
            fixture.IsOnlyOneWord = "Foo";
        }

        Assert.Multiple(() =>
        {
            Assert.That(fixture.AreChangeNotificationsEnabled(), Is.True);
            Assert.That(fixture.IsOnlyOneWord, Is.EqualTo("Foo"), "The value is still assigned.");
            Assert.That(changed, Is.Empty, "Suppressed notifications are dropped, never replayed.");
        });
    }

    [Test]
    public void SuppressChangeNotificationsNests()
    {
        var fixture = new TestFixture();

        var outer = fixture.SuppressChangeNotifications();
        var inner = fixture.SuppressChangeNotifications();

        inner.Dispose();
        Assert.That(fixture.AreChangeNotificationsEnabled(), Is.False, "The outer scope is still open.");

        outer.Dispose();
        Assert.That(fixture.AreChangeNotificationsEnabled(), Is.True);
    }

    [Test]
    public void DelayedNotificationsDoNotShowUpUntilUndelayed()
    {
        var fixture = new TestFixture();
        var changing = new List<string?>();
        var changed = new List<string?>();
        var propertyChangingEvents = new List<string?>();
        var propertyChangedEvents = new List<string?>();

        fixture.Changing.Subscribe(args => changing.Add(args.PropertyName));
        fixture.Changed.Subscribe(args => changed.Add(args.PropertyName));
        fixture.PropertyChanging += (_, args) => propertyChangingEvents.Add(args.PropertyName);
        fixture.PropertyChanged += (_, args) => propertyChangedEvents.Add(args.PropertyName);

        fixture.NullableInt = 4;
        AssertCount(1);

        var stopDelaying = fixture.DelayChangeNotifications();

        fixture.NullableInt = 5;
        fixture.IsNotNullString = "Bar";
        fixture.NullableInt = 6;
        fixture.IsNotNullString = "Baz";
        AssertCount(1);

        var stopDelayingMore = fixture.DelayChangeNotifications();
        fixture.IsNotNullString = "Bamf";
        AssertCount(1);

        stopDelaying.Dispose();
        fixture.IsNotNullString = "Blargh";
        AssertCount(1, "The outer delay scope is still open.");

        stopDelayingMore.Dispose();

        // Repeated changes to one property collapse into a single notification carrying the final value.
        string?[] expected =
        [
            nameof(TestFixture.NullableInt),
            nameof(TestFixture.NullableInt),
            nameof(TestFixture.IsNotNullString),
        ];

        Assert.Multiple(() =>
        {
            Assert.That(changing, Is.EqualTo(expected));
            Assert.That(changed, Is.EqualTo(expected));
            Assert.That(propertyChangingEvents, Is.EqualTo(expected));
            Assert.That(propertyChangedEvents, Is.EqualTo(expected));
            Assert.That(fixture.NullableInt, Is.EqualTo(6));
            Assert.That(fixture.IsNotNullString, Is.EqualTo("Blargh"));
        });

        void AssertCount(int expectedCount, string? because = null)
        {
            Assert.Multiple(() =>
            {
                Assert.That(changing, Has.Count.EqualTo(expectedCount), because);
                Assert.That(changed, Has.Count.EqualTo(expectedCount), because);
                Assert.That(propertyChangingEvents, Has.Count.EqualTo(expectedCount), because);
                Assert.That(propertyChangedEvents, Has.Count.EqualTo(expectedCount), because);
            });
        }
    }

    [Test]
    public void ExceptionsInSubscribersPropagateToTheSetter()
    {
        // ReactiveUI routes these into ReactiveObject.ThrownExceptions; this library deliberately lets them
        // out, so a broken subscriber fails loudly at the assignment that triggered it.
        var fixture = new TestFixture();
        fixture.Changed.Subscribe(static _ => throw new InvalidOperationException("Die!"));

        Assert.Throws<InvalidOperationException>(() => fixture.IsOnlyOneWord = "Bar");
    }

    [Test]
    public void ChangedIsOnlyAllocatedWhenObserved()
    {
        // Nothing to assert beyond "this does not throw": the subjects are created lazily, so raising before
        // anyone subscribes must still work.
        var fixture = new TestFixture();
        Assert.DoesNotThrow(() => fixture.IsOnlyOneWord = "Foo");
    }

    [Test]
    public void RaisePropertyChangedCanBeCalledDirectlyForComputedProperties()
    {
        var fixture = new TestFixture();
        var changed = new List<string?>();
        fixture.Changed.Subscribe(args => changed.Add(args.PropertyName));

        fixture.RaisePropertyChanged("Computed");

        Assert.That(changed, Is.EqualTo(new[] { "Computed" }));
    }

    [Test]
    public void ImplementsTheBclChangeNotificationInterfaces()
    {
        var fixture = new TestFixture();

        Assert.Multiple(() =>
        {
            Assert.That(fixture, Is.InstanceOf<INotifyPropertyChanged>());
            Assert.That(fixture, Is.InstanceOf<INotifyPropertyChanging>());
        });
    }
}