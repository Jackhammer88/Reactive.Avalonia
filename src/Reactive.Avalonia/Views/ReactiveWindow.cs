using Avalonia;
using Avalonia.Controls;

namespace Reactive.Avalonia;

/// <summary>
/// A <see cref="Window"/> bound to a view model, with activation wired up.
/// </summary>
/// <typeparam name="TViewModel">The view model type.</typeparam>
/// <remarks>
/// <see cref="ViewModel"/> and <see cref="StyledElement.DataContext"/> track each other, so XAML bindings and
/// typed code-behind access work at the same time. If the view model implements
/// <see cref="IActivatableViewModel"/>, it is activated while the window is loaded.
/// </remarks>
/// <example>
/// <code language="csharp">
/// public partial class MainWindow : ReactiveWindow&lt;MainViewModel&gt;
/// {
///     public MainWindow()
///     {
///         InitializeComponent();
///         this.WhenActivated(disposables => { /* subscriptions */ });
///     }
/// }
/// </code>
/// </example>
public class ReactiveWindow<TViewModel> : Window, IViewFor<TViewModel>, IActivatableView
    where TViewModel : class
{
    /// <summary>
    /// Defines the <see cref="ViewModel"/> property.
    /// </summary>
    [SuppressMessage(
        "AvaloniaProperty",
        "AVP1002:AvaloniaProperty objects should not be owned by a generic type",
        Justification = "The view model type is exactly what makes this base class useful.")]
    public static readonly StyledProperty<TViewModel?> ViewModelProperty =
        AvaloniaProperty.Register<ReactiveWindow<TViewModel>, TViewModel?>(nameof(ViewModel));

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactiveWindow{TViewModel}"/> class.
    /// </summary>
    public ReactiveWindow() => this.WhenActivated(static _ => { });

    /// <summary>
    /// Gets or sets the view model shown by this window.
    /// </summary>
    public TViewModel? ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    /// <inheritdoc/>
    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (TViewModel?)value;
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        ViewModelBinding.Sync(this, change, ViewModelProperty);
    }
}