namespace Reactive.Avalonia.Tests;

/// <summary>
/// Covers the fallback-observer behaviour that makes unobserved errors loud.
/// </summary>
/// <remarks>
/// <c>ScheduledSubject</c> itself is internal, so these exercise it through
/// <see cref="ObservableAsPropertyHelper{T}.ThrownExceptions"/>, which is its only public surface.
/// Adapted from ReactiveUI's <c>ScheduledSubjectTest</c>.
/// </remarks>
[TestFixture]
public class ScheduledSubjectTests : ReactiveTestBase
{
    [Test]
    public void ValuesGoToTheFallbackWhileNobodyIsSubscribed()
    {
        var handler = new RecordingHandler();
        var previous = RxSchedulers.DefaultExceptionHandler;
        RxSchedulers.DefaultExceptionHandler = handler;

        try
        {
            var input = new Subject<int>();
            var helper = Observable.Return(0).Concat(input).ToProperty(new Host(), "Value");

            input.OnError(new InvalidOperationException("boom"));

            Assert.That(handler.Received, Has.Count.EqualTo(1));
            helper.Dispose();
        }
        finally
        {
            RxSchedulers.DefaultExceptionHandler = previous;
        }
    }

    [Test]
    public void TheFallbackStepsAsideOnceSomebodySubscribes()
    {
        var handler = new RecordingHandler();
        var previous = RxSchedulers.DefaultExceptionHandler;
        RxSchedulers.DefaultExceptionHandler = handler;

        try
        {
            var input = new Subject<int>();
            var helper = Observable.Return(0).Concat(input).ToProperty(new Host(), "Value");
            var observed = new List<Exception>();

            using (helper.ThrownExceptions.Subscribe(observed.Add))
            {
                input.OnError(new InvalidOperationException("boom"));
            }

            Assert.Multiple(() =>
            {
                Assert.That(observed, Has.Count.EqualTo(1));
                Assert.That(handler.Received, Is.Empty, "The fallback must not double-report.");
            });

            helper.Dispose();
        }
        finally
        {
            RxSchedulers.DefaultExceptionHandler = previous;
        }
    }

    [Test]
    public void ObserversAreNotifiedOnTheSuppliedScheduler()
    {
        var scheduler = new Microsoft.Reactive.Testing.TestScheduler();
        var input = new Subject<int>();
        var host = new Host();
        var helper = input.ToProperty(host, "Value", scheduler);
        var observed = new List<Exception>();

        helper.ThrownExceptions.Subscribe(observed.Add);
        input.OnError(new InvalidOperationException("boom"));

        Assert.That(observed, Is.Empty, "Nothing has been pumped yet.");

        scheduler.Start();
        Assert.That(observed, Has.Count.EqualTo(1));

        helper.Dispose();
    }

    private sealed class Host : ReactiveObject
    {
        public int Value => 0;
    }

    private sealed class RecordingHandler : IObserver<Exception>
    {
        public List<Exception> Received { get; } = [];

        public void OnCompleted()
        {
        }

        public void OnError(Exception error) => Received.Add(error);

        public void OnNext(Exception value) => Received.Add(value);
    }
}