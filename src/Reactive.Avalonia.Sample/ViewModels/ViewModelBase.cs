namespace Reactive.Avalonia.Sample.ViewModels;

/// <summary>
/// The base class for this sample's view models.
/// </summary>
/// <remarks>
/// <see cref="ReactiveValidationObject"/> is <see cref="ReactiveObject"/> plus the
/// <see cref="System.ComponentModel.INotifyDataErrorInfo"/> plumbing, so any view model here can declare
/// validation rules without extra ceremony.
/// </remarks>
public abstract class ViewModelBase : ReactiveValidationObject
{
}