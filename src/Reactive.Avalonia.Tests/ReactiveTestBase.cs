namespace Reactive.Avalonia.Tests;

/// <summary>
/// Pins the library's schedulers to <see cref="ImmediateScheduler"/> for the duration of each test.
/// </summary>
/// <remarks>
/// The production default is <see cref="AvaloniaScheduler"/>, which posts to a dispatcher that never pumps in a
/// plain test host — anything scheduled there would silently never run. Immediate scheduling also makes the
/// tests synchronous, so no test has to poll or sleep.
/// </remarks>
public abstract class ReactiveTestBase
{
    private IDisposable? _schedulers;

    [SetUp]
    public void PinSchedulers() => _schedulers = RxSchedulers.With(ImmediateScheduler.Instance);

    [TearDown]
    public void RestoreSchedulers()
    {
        _schedulers?.Dispose();
        _schedulers = null;
    }
}