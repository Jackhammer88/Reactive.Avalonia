using System.Reactive.Threading.Tasks;

namespace Reactive.Avalonia.Tests;

/// <summary>
/// Covers <see cref="RxSchedulers"/>.
/// </summary>
/// <remarks>Adapted from ReactiveUI's <c>RxSchedulersTest</c>.</remarks>
[TestFixture]
public class RxSchedulersTests
{
    [Test]
    public void DefaultsToTheAvaloniaDispatcher()
    {
        // Deliberately not derived from ReactiveTestBase: this asserts the untouched defaults.
        Assert.Multiple(() =>
        {
            Assert.That(RxSchedulers.MainThreadScheduler, Is.SameAs(AvaloniaScheduler.Instance));
            Assert.That(RxSchedulers.TaskpoolScheduler, Is.SameAs(TaskPoolScheduler.Default));
        });
    }

    [Test]
    public void SchedulersCanBeReplaced()
    {
        var previousMain = RxSchedulers.MainThreadScheduler;
        var previousTaskpool = RxSchedulers.TaskpoolScheduler;

        try
        {
            RxSchedulers.MainThreadScheduler = ImmediateScheduler.Instance;
            RxSchedulers.TaskpoolScheduler = CurrentThreadScheduler.Instance;

            Assert.Multiple(() =>
            {
                Assert.That(RxSchedulers.MainThreadScheduler, Is.SameAs(ImmediateScheduler.Instance));
                Assert.That(RxSchedulers.TaskpoolScheduler, Is.SameAs(CurrentThreadScheduler.Instance));
            });
        }
        finally
        {
            RxSchedulers.MainThreadScheduler = previousMain;
            RxSchedulers.TaskpoolScheduler = previousTaskpool;
        }
    }

    [Test]
    public void WithRestoresBothSchedulers()
    {
        var previousMain = RxSchedulers.MainThreadScheduler;
        var previousTaskpool = RxSchedulers.TaskpoolScheduler;

        using (RxSchedulers.With(ImmediateScheduler.Instance))
        {
            Assert.Multiple(() =>
            {
                Assert.That(RxSchedulers.MainThreadScheduler, Is.SameAs(ImmediateScheduler.Instance));
                Assert.That(
                    RxSchedulers.TaskpoolScheduler,
                    Is.SameAs(ImmediateScheduler.Instance),
                    "One argument pins both.");
            });
        }

        Assert.Multiple(() =>
        {
            Assert.That(RxSchedulers.MainThreadScheduler, Is.SameAs(previousMain));
            Assert.That(RxSchedulers.TaskpoolScheduler, Is.SameAs(previousTaskpool));
        });
    }

    [Test]
    public void WithCanPinTheTwoSchedulersSeparately()
    {
        using (RxSchedulers.With(ImmediateScheduler.Instance, CurrentThreadScheduler.Instance))
        {
            Assert.Multiple(() =>
            {
                Assert.That(RxSchedulers.MainThreadScheduler, Is.SameAs(ImmediateScheduler.Instance));
                Assert.That(RxSchedulers.TaskpoolScheduler, Is.SameAs(CurrentThreadScheduler.Instance));
            });
        }
    }

    [Test]
    public void UseCurrentThreadPointsAwayFromTheDispatcher()
    {
        var previousMain = RxSchedulers.MainThreadScheduler;
        var previousTaskpool = RxSchedulers.TaskpoolScheduler;

        try
        {
            RxSchedulers.UseCurrentThread();

            Assert.Multiple(() =>
            {
                Assert.That(RxSchedulers.MainThreadScheduler, Is.SameAs(CurrentThreadScheduler.Instance));
                Assert.That(RxSchedulers.TaskpoolScheduler, Is.SameAs(TaskPoolScheduler.Default));
            });
        }
        finally
        {
            RxSchedulers.MainThreadScheduler = previousMain;
            RxSchedulers.TaskpoolScheduler = previousTaskpool;
        }
    }

    [Test]
    public async Task UseCurrentThreadLetsACommandBeAwaitedWithNoDispatcher()
    {
        // This is the failure it exists to prevent: with the dispatcher default and nothing pumping it, the
        // await below never returns.
        var previousMain = RxSchedulers.MainThreadScheduler;
        var previousTaskpool = RxSchedulers.TaskpoolScheduler;

        try
        {
            RxSchedulers.UseCurrentThread();

            using var command = ReactiveCommand.CreateFromTask(static async () =>
            {
                await Task.Yield();
                return 42;
            });

            var execution = command.Execute().FirstAsync().ToTask();
            var finished = await Task.WhenAny(execution, Task.Delay(TimeSpan.FromSeconds(10)));

            Assert.That(finished, Is.SameAs(execution), "The command never delivered its result.");
            Assert.That(await execution, Is.EqualTo(42));
        }
        finally
        {
            RxSchedulers.MainThreadScheduler = previousMain;
            RxSchedulers.TaskpoolScheduler = previousTaskpool;
        }
    }

    [Test]
    public void RejectsNullSchedulers()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(static () => RxSchedulers.MainThreadScheduler = null!);
            Assert.Throws<ArgumentNullException>(static () => RxSchedulers.TaskpoolScheduler = null!);
            Assert.Throws<ArgumentNullException>(static () => RxSchedulers.DefaultExceptionHandler = null!);
            Assert.Throws<ArgumentNullException>(static () => RxSchedulers.With(null!));
        });
    }

    [Test]
    public void TheDefaultExceptionHandlerRethrowsWrapped()
    {
        using var _ = RxSchedulers.With(ImmediateScheduler.Instance);

        var exception = Assert.Throws<UnhandledErrorException>(
            static () => RxSchedulers.DefaultExceptionHandler.OnNext(new InvalidOperationException("boom")));

        Assert.That(exception!.InnerException?.Message, Is.EqualTo("boom"));
    }
}