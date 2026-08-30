namespace Reactive.Avalonia.Tests.Mocks;

/// <summary>
/// An object that raises no change notifications at all.
/// </summary>
public class NonObservableTestFixture
{
    public string? IsNotNullString { get; set; }
}