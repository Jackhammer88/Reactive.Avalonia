// The ViewModel/DataContext synchronisation is derived from ReactiveUI.Avalonia 11.4.13.
// Copyright (c) 2019-2026 ReactiveUI and Avalonia Teams, and Contributors. Licensed under the MIT license.
// See THIRD-PARTY-NOTICES.md in the repository root.

using Avalonia;
using Avalonia.Controls;

namespace Reactive.Avalonia;

/// <summary>
/// Keeps a view's <c>ViewModel</c> property and its <see cref="StyledElement.DataContext"/> in step.
/// </summary>
/// <remarks>
/// Shared by <see cref="ReactiveWindow{TViewModel}"/> and <see cref="ReactiveView{TViewModel}"/>, which cannot
/// share a base class because one is a <see cref="Window"/> and the other a <see cref="UserControl"/>.
/// </remarks>
internal static class ViewModelBinding
{
    /// <summary>
    /// Mirrors a property change between <c>ViewModel</c> and <see cref="StyledElement.DataContext"/>.
    /// </summary>
    /// <typeparam name="TViewModel">The view model type.</typeparam>
    /// <param name="view">The view being updated.</param>
    /// <param name="change">The change reported by Avalonia.</param>
    /// <param name="viewModelProperty">The view's <c>ViewModel</c> property.</param>
    /// <remarks>
    /// Each direction only writes when the two were already in agreement, so an explicitly assigned
    /// <c>ViewModel</c> is not clobbered by an inherited <see cref="StyledElement.DataContext"/>, and a
    /// <see cref="StyledElement.DataContext"/> holding some other type is left alone.
    /// </remarks>
    public static void Sync<TViewModel>(
        AvaloniaObject view,
        AvaloniaPropertyChangedEventArgs change,
        StyledProperty<TViewModel?> viewModelProperty)
        where TViewModel : class
    {
        if (change.Property == StyledElement.DataContextProperty)
        {
            if (ReferenceEquals(change.OldValue, view.GetValue(viewModelProperty)) &&
                change.NewValue is null or TViewModel)
            {
                view.SetCurrentValue(viewModelProperty, change.NewValue);
            }
        }
        else if (change.Property == viewModelProperty)
        {
            if (ReferenceEquals(change.OldValue, view.GetValue(StyledElement.DataContextProperty)))
            {
                view.SetCurrentValue(StyledElement.DataContextProperty, change.NewValue);
            }
        }
    }
}