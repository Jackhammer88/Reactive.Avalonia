using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;

using Reactive.Avalonia.Sample.Abstractions;

namespace Reactive.Avalonia.Sample.ViewModels;

/// <summary>
/// Shows the four things this library exists for: a property fed by an observable, a validated input, a command
/// gated on that validation, and subscriptions scoped to the view's lifetime.
/// </summary>
public sealed class MainViewModel : ViewModelBase, IActivatableViewModel
{
    private readonly ObservableAsPropertyHelper<string> _text;

    public MainViewModel()
    {
        // A read-only property driven by a background sequence. ToProperty marshals to the UI thread for us.
        Observable.Interval(TimeSpan.FromMilliseconds(500))
                  .Select(tick => $"Hello {tick}")
                  .ToProperty(this, vm => vm.Text, out _text);

        this.ValidationRule(
            vm => vm.Name,
            name => !string.IsNullOrWhiteSpace(name),
            "Name is required.");

        this.ValidationRule(
            vm => vm.Name,
            name => name.Length <= 20,
            name => $"Name is {name.Length} characters; 20 is the limit.");

        Greet = ReactiveCommand.Create(
            () => { LastGreeting = $"Hi, {Name}!"; },
            this.IsValid());

        this.WhenActivated(disposables =>
            this.WhenAnyValue(vm => vm.Text)
                .Subscribe(text => Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {text}"))
                .DisposeWith(disposables));
    }

    public IReadOnlyCollection<TabItemViewModelBase> TabVms { get; } =
    [
        new ValidationViewModel(),
        new CommandViewModel(),
        new InteractionViewModel()
    ];

    /// <inheritdoc/>
    public ViewModelActivator Activator { get; } = new();

    /// <summary>Gets the ticking text produced by the interval sequence.</summary>
    public string Text => _text.Value;

    /// <summary>Gets or sets the name to greet. Validated on every keystroke.</summary>
    public string Name
    {
        get;
        set => RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    /// <summary>Gets the greeting produced by the last successful <see cref="Greet"/>.</summary>
    public string LastGreeting
    {
        get;
        private set => RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    /// <summary>Gets the command that greets <see cref="Name"/>.
    /// Disabled while validation fails.</summary>
    public ReactiveCommand<Unit, Unit> Greet { get; }

    public TabItemViewModelBase? SelectedTab
    {
        get;
        set
        {
            if (ReferenceEquals(field, value))
                return;

            if (field is IActivatableTab oldTab)
                oldTab.IsActive = false;

            field = value;

            if (field is IActivatableTab newTab)
                newTab.IsActive = true;
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _text.Dispose();
            Greet.Dispose();
            Activator.Dispose();
        }

        base.Dispose(disposing);
    }
}