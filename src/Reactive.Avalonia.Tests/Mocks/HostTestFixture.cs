namespace Reactive.Avalonia.Tests.Mocks;

/// <summary>
/// The root of a two-level property chain, used to check that observation follows a replaced link.
/// </summary>
public class HostTestFixture : ReactiveObject
{
    private TestFixture? _child;
    private NonObservableTestFixture? _pocoChild;

    public TestFixture? Child
    {
        get => _child;
        set => this.RaiseAndSetIfChanged(ref _child, value);
    }

    public NonObservableTestFixture? PocoChild
    {
        get => _pocoChild;
        set => this.RaiseAndSetIfChanged(ref _pocoChild, value);
    }
}