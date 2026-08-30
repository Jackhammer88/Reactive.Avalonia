using System.ComponentModel;

namespace Reactive.Avalonia.Tests.Mocks;

/// <summary>
/// An <see cref="IReactiveObject"/> written by hand, for types that already have a base class.
/// </summary>
public class HandRolledReactiveObject : IReactiveObject
{
    private readonly Subject<PropertyChangedEventArgs> _changed = new();
    private readonly Subject<PropertyChangingEventArgs> _changing = new();
    private string? _name;

    public event PropertyChangedEventHandler? PropertyChanged;

    public event PropertyChangingEventHandler? PropertyChanging;

    public IObservable<PropertyChangedEventArgs> Changed => _changed;

    public IObservable<PropertyChangingEventArgs> Changing => _changing;

    public string? Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public void RaisePropertyChanged(string? propertyName = null)
    {
        var args = new PropertyChangedEventArgs(propertyName);
        PropertyChanged?.Invoke(this, args);
        _changed.OnNext(args);
    }

    public void RaisePropertyChanging(string? propertyName = null)
    {
        var args = new PropertyChangingEventArgs(propertyName);
        PropertyChanging?.Invoke(this, args);
        _changing.OnNext(args);
    }
}