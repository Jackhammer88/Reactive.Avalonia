namespace Reactive.Avalonia.Tests;

/// <summary>
/// Covers <see cref="Interaction{TInput, TOutput}"/> and <see cref="InteractionContext{TInput, TOutput}"/>.
/// </summary>
/// <remarks>Adapted from ReactiveUI's <c>InteractionsTest</c>.</remarks>
[TestFixture]
public class InteractionTests : ReactiveTestBase
{
    [Test]
    public void AHandlerAnswersTheInteraction()
    {
        var interaction = new Interaction<string, bool>();
        using var _ = interaction.RegisterHandler(context => context.SetOutput(context.Input == "yes"));

        bool? answer = null;
        interaction.Handle("yes").Subscribe(value => answer = value);

        Assert.That(answer, Is.True);
    }

    [Test]
    public void AnUnhandledInteractionFails()
    {
        var interaction = new Interaction<string, bool>();
        Exception? caught = null;

        interaction.Handle("x").Subscribe(static _ => { }, ex => caught = ex);

        Assert.That(caught, Is.TypeOf<UnhandledInteractionException>());
    }

    [Test]
    public void AnInteractionIsUnhandledOnceItsHandlerIsUnregistered()
    {
        var interaction = new Interaction<string, bool>();
        var registration = interaction.RegisterHandler(static context => context.SetOutput(true));

        registration.Dispose();

        Exception? caught = null;
        interaction.Handle("x").Subscribe(static _ => { }, ex => caught = ex);

        Assert.That(caught, Is.TypeOf<UnhandledInteractionException>());
    }

    [Test]
    public void HandlersRunInReverseOrderOfRegistration()
    {
        var interaction = new Interaction<string, string>();
        var order = new List<string>();

        using var first = interaction.RegisterHandler(context =>
        {
            order.Add("first");
            context.SetOutput("first");
        });

        using var second = interaction.RegisterHandler(context =>
        {
            order.Add("second");
            context.SetOutput("second");
        });

        string? answer = null;
        interaction.Handle("x").Subscribe(value => answer = value);

        Assert.Multiple(() =>
        {
            Assert.That(answer, Is.EqualTo("second"), "The newest handler wins.");
            Assert.That(order, Is.EqualTo(new[] { "second" }), "Earlier handlers are never consulted.");
        });
    }

    [Test]
    public void AHandlerCanDeclineToAnswer()
    {
        var interaction = new Interaction<string, string>();
        var consulted = new List<string>();

        using var first = interaction.RegisterHandler(context =>
        {
            consulted.Add("first");
            context.SetOutput("first");
        });

        using var second = interaction.RegisterHandler(context => consulted.Add("second"));

        string? answer = null;
        interaction.Handle("x").Subscribe(value => answer = value);

        Assert.Multiple(() =>
        {
            Assert.That(consulted, Is.EqualTo(new[] { "second", "first" }));
            Assert.That(answer, Is.EqualTo("first"));
        });
    }

    [Test]
    public async Task HandlersCanBeAsynchronous()
    {
        var interaction = new Interaction<string, string>();
        using var _ = interaction.RegisterHandler(async context =>
        {
            await Task.Yield();
            context.SetOutput($"handled {context.Input}");
        });

        var answer = await interaction.Handle("x");

        Assert.That(answer, Is.EqualTo("handled x"));
    }

    [Test]
    public void HandlersCanBeObservable()
    {
        var interaction = new Interaction<string, string>();
        var gate = new Subject<Unit>();

        using var _ = interaction.RegisterHandler(context =>
            gate.Take(1).Do(_ => context.SetOutput("late")).Select(static _ => Unit.Default));

        string? answer = null;
        interaction.Handle("x").Subscribe(value => answer = value);

        Assert.That(answer, Is.Null, "The handler has not finished yet.");

        gate.OnNext(Unit.Default);

        Assert.That(answer, Is.EqualTo("late"));
    }

    [Test]
    public void ANestedInteractionIsNotBlockedByItsParent()
    {
        var parent = new Interaction<Unit, Unit>();
        var nested = new Interaction<Unit, string>();
        string? nestedAnswer = null;

        using var nestedHandler = nested.RegisterHandler(static context => context.SetOutput("nested"));
        using var parentHandler = parent.RegisterHandler(context =>
        {
            nested.Handle(Unit.Default).Subscribe(value => nestedAnswer = value);
            context.SetOutput(Unit.Default);
        });

        parent.Handle(Unit.Default).Subscribe();

        Assert.That(nestedAnswer, Is.EqualTo("nested"));
    }

    [Test]
    public void ReadingTheOutputBeforeItIsSetFails()
    {
        var interaction = new Interaction<Unit, Unit>();
        using var _ = interaction.RegisterHandler(static context => context.GetOutput());

        Exception? caught = null;
        interaction.Handle(Unit.Default).Subscribe(static _ => { }, ex => caught = ex);

        Assert.Multiple(() =>
        {
            Assert.That(caught, Is.TypeOf<InvalidOperationException>());
            Assert.That(caught!.Message, Is.EqualTo("Output has not been set."));
        });
    }

    [Test]
    public void AnsweringTwiceFails()
    {
        var interaction = new Interaction<Unit, Unit>();
        using var _ = interaction.RegisterHandler(static context =>
        {
            context.SetOutput(Unit.Default);
            context.SetOutput(Unit.Default);
        });

        Exception? caught = null;
        interaction.Handle(Unit.Default).Subscribe(static _ => { }, ex => caught = ex);

        Assert.Multiple(() =>
        {
            Assert.That(caught, Is.TypeOf<InvalidOperationException>());
            Assert.That(caught!.Message, Is.EqualTo("Output has already been set."));
        });
    }

    [Test]
    public void HandlersRunOnTheSuppliedScheduler()
    {
        using var scheduler = new EventLoopScheduler();
        var interaction = new Interaction<Unit, int>(scheduler);
        var schedulerThreadId = 0;
        scheduler.Schedule(() => schedulerThreadId = Environment.CurrentManagedThreadId);

        using var _ = interaction.RegisterHandler(static context =>
            context.SetOutput(Environment.CurrentManagedThreadId));

        var handlerThreadId = interaction.Handle(Unit.Default).Wait();

        Assert.Multiple(() =>
        {
            Assert.That(schedulerThreadId, Is.Not.Zero);
            Assert.That(handlerThreadId, Is.EqualTo(schedulerThreadId));
        });
    }

    [Test]
    public void RejectsANullHandler()
    {
        var interaction = new Interaction<Unit, Unit>();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                interaction.RegisterHandler((Action<InteractionContext<Unit, Unit>>)null!));
            Assert.Throws<ArgumentNullException>(() =>
                interaction.RegisterHandler((Func<InteractionContext<Unit, Unit>, Task>)null!));
            Assert.Throws<ArgumentNullException>(() =>
                interaction.RegisterHandler((Func<InteractionContext<Unit, Unit>, IObservable<Unit>>)null!));
        });
    }

    [Test]
    public void ContextExposesTheInputAndHandledState()
    {
        var interaction = new Interaction<int, int>();
        var wasHandledDuringHandler = true;

        using var _ = interaction.RegisterHandler(context =>
        {
            wasHandledDuringHandler = context.IsHandled;
            context.SetOutput(context.Input * 2);
        });

        var answer = interaction.Handle(21).Wait();

        Assert.Multiple(() =>
        {
            Assert.That(wasHandledDuringHandler, Is.False);
            Assert.That(answer, Is.EqualTo(42));
        });
    }
}