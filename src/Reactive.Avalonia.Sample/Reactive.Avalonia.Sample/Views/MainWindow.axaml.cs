using System;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;

using Avalonia.Input;

using Reactive.Avalonia.Sample.ViewModels;

namespace Reactive.Avalonia.Sample.Views;

public partial class MainWindow : ReactiveWindow<MainViewModel>
{
    public MainWindow()
    {
        InitializeComponent();

        // Everything registered here is disposed when the window leaves the visual tree, and set up again if
        // it comes back. The view model's own WhenActivated block is driven by the same signal.
        this.WhenActivated(disposables =>
        {
            Observable.FromEventPattern<PointerEventArgs>(
                    handler => PointerMoved += handler,
                    handler => PointerMoved -= handler)
                .Sample(TimeSpan.FromSeconds(1))

                // Sample ticks on a timer thread. Touching a Visual from there throws, so hop back.
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(pattern => Console.WriteLine($"Pointer at {pattern.EventArgs.GetPosition(this)}"))
                .DisposeWith(disposables);
        });
    }
}