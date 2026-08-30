namespace Reactive.Avalonia;

/// <summary>
/// A view that can drive activation: <c>this.WhenActivated(...)</c> hooks its visual-tree lifetime.
/// </summary>
/// <remarks>
/// <see cref="ReactiveWindow{TViewModel}"/> and <see cref="ReactiveView{TViewModel}"/> already implement this.
/// Add it to your own Avalonia control if you need activation without those base classes.
/// </remarks>
public interface IActivatableView;