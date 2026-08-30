namespace Reactive.Avalonia.Tests.Mocks;

/// <summary>
/// A plain <see cref="ReactiveObject"/> with a property of each shape the property machinery has to cope with.
/// </summary>
public class TestFixture : ReactiveObject
{
    private string? _isNotNullString;
    private string? _isOnlyOneWord;
    private int? _nullableInt;
    private int _notNullableInt;
    private string? _usesExprRaiseSet;

    public string? IsNotNullString
    {
        get => _isNotNullString;
        set => this.RaiseAndSetIfChanged(ref _isNotNullString, value);
    }

    public string? IsOnlyOneWord
    {
        get => _isOnlyOneWord;
        set => this.RaiseAndSetIfChanged(ref _isOnlyOneWord, value);
    }

    public int? NullableInt
    {
        get => _nullableInt;
        set => this.RaiseAndSetIfChanged(ref _nullableInt, value);
    }

    public int NotNullableInt
    {
        get => _notNullableInt;
        set => this.RaiseAndSetIfChanged(ref _notNullableInt, value);
    }

    public string? UsesExprRaiseSet
    {
        get => _usesExprRaiseSet;
        set => this.RaiseAndSetIfChanged(ref _usesExprRaiseSet, value);
    }

    /// <summary>
    /// Gets or sets a property that raises nothing at all.
    /// </summary>
    public string? PocoProperty { get; set; }
}