using Reactive.Avalonia.Sample.Abstractions;

namespace Reactive.Avalonia.Sample.ViewModels;

public abstract class TabItemViewModelBase : ViewModelBase, IActivatableTab
{
    public abstract string Title { get; }

    public bool IsActive => _isActive;

    private bool _isActive;

    bool IActivatableTab.IsActive
    {
        get => _isActive;
        set => RaiseAndSetIfChanged(ref _isActive, value);
    }
}