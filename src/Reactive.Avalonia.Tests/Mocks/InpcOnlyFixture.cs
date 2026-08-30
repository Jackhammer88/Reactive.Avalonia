using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Reactive.Avalonia.Tests.Mocks;

/// <summary>
/// An <see cref="INotifyPropertyChanged"/> implementation that knows nothing about this library, to prove the
/// property machinery only needs the BCL interfaces.
/// </summary>
public class InpcOnlyFixture : INotifyPropertyChanged, INotifyPropertyChanging
{
    private string? _property;

    public event PropertyChangedEventHandler? PropertyChanged;

    public event PropertyChangingEventHandler? PropertyChanging;

    public string? Property
    {
        get => _property;
        set
        {
            if (_property == value)
            {
                return;
            }

            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(nameof(Property)));
            _property = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property)));
        }
    }

    /// <summary>
    /// Raises a change notification with no property name, meaning "everything changed".
    /// </summary>
    public void RaiseWholeObjectChanged()
    {
        PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(string.Empty));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }

    /// <summary>
    /// Sets the backing field directly without raising anything.
    /// </summary>
    /// <param name="value">The value to store.</param>
    public void SetPropertySilently(string? value) => _property = value;
}