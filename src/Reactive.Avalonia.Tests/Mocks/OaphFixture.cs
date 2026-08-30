namespace Reactive.Avalonia.Tests.Mocks;

/// <summary>
/// A view model whose read-only property is driven by an injected sequence.
/// </summary>
public class OaphFixture : ReactiveObject
{
    private readonly ObservableAsPropertyHelper<int> _value;

    public OaphFixture(IObservable<int> source, int initialValue = -5, IScheduler? scheduler = null) =>
        source.ToProperty(this, x => x.Value, out _value, initialValue, scheduler);

    public int Value => _value.Value;

    public ObservableAsPropertyHelper<int> Helper => _value;
}