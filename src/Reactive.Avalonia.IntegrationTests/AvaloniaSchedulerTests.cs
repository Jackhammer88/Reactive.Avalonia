using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;

namespace Reactive.Avalonia.IntegrationTests;

/// <summary>
/// Checks that <see cref="AvaloniaScheduler"/> puts work on the dispatcher thread.
/// </summary>
/// <remarks>
/// This is the reason a view model can subscribe to a background sequence and still touch bound properties.
/// </remarks>
[TestFixture]
public class AvaloniaSchedulerTests
{
    [AvaloniaTest]
    public void IsTheDefaultMainThreadScheduler()
    {
        Assert.That(RxSchedulers.MainThreadScheduler, Is.SameAs(AvaloniaScheduler.Instance));
    }

    [AvaloniaTest]
    public void RunsInlineWhenAlreadyOnTheDispatcherThread()
    {
        var ran = false;

        AvaloniaScheduler.Instance.Schedule(() => ran = true);

        Assert.That(ran, Is.True, "Inlining is what keeps reactive chains cheap on the UI thread.");
    }

    [AvaloniaTest]
    public void PostsWorkArrivingFromAnotherThread()
    {
        var uiThreadId = Environment.CurrentManagedThreadId;
        var observedThreadId = 0;

        Task.Run(() => AvaloniaScheduler.Instance.Schedule(() => observedThreadId = Environment.CurrentManagedThreadId))
            .Wait(TimeSpan.FromSeconds(5));

        Dispatcher.UIThread.RunJobs();

        Assert.That(observedThreadId, Is.EqualTo(uiThreadId));
    }

    [AvaloniaTest]
    public void ToPropertyMarshalsABackgroundSequenceOntoTheDispatcher()
    {
        var uiThreadId = Environment.CurrentManagedThreadId;
        var input = new Subject<int>();
        var host = new BackgroundFedViewModel(input);
        var notifiedThreadId = 0;

        host.Changed.Subscribe(_ => notifiedThreadId = Environment.CurrentManagedThreadId);

        Task.Run(() => input.OnNext(42)).Wait(TimeSpan.FromSeconds(5));
        Dispatcher.UIThread.RunJobs();

        Assert.Multiple(() =>
        {
            Assert.That(host.Value, Is.EqualTo(42));
            Assert.That(notifiedThreadId, Is.EqualTo(uiThreadId));
        });
    }

    [AvaloniaTest]
    public void ABoundControlSeesTheBackgroundValue()
    {
        var input = new Subject<int>();
        var host = new BackgroundFedViewModel(input);
        var text = new TextBlock();
        text.Bind(TextBlock.TextProperty, new Binding(nameof(BackgroundFedViewModel.Value)));
        text.DataContext = host;

        var window = new Window { Content = text };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Task.Run(() => input.OnNext(7)).Wait(TimeSpan.FromSeconds(5));
        Dispatcher.UIThread.RunJobs();

        Assert.That(text.Text, Is.EqualTo("7"));
    }

    private sealed class BackgroundFedViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<int> _value;

        public BackgroundFedViewModel(IObservable<int> source) =>
            source.ToProperty(this, x => x.Value, out _value);

        public int Value => _value.Value;
    }
}