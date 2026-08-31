using System.ComponentModel;

namespace Reactive.Avalonia.Tests.Mocks;

/// <summary>
/// Reports changes but not upcoming changes, so before-change observation cannot work on it.
/// </summary>
public class ChangedOnlyFixture : INotifyPropertyChanged
{
    private string? _value;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string? Value
    {
        get => _value;
        set
        {
            _value = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }
}
