# Reactive.Avalonia

[![CI](https://github.com/Jackhammer88/Reactive.Avalonia/actions/workflows/ci.yml/badge.svg)](https://github.com/Jackhammer88/Reactive.Avalonia/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Reactive.Avalonia.svg)](https://www.nuget.org/packages/Reactive.Avalonia)

Reactive MVVM for Avalonia: observable-driven properties, commands, validation and view activation — on
.NET 10, with nothing that trimming or NativeAOT can break.

```
dotnet add package Reactive.Avalonia
```

> **Not affiliated with ReactiveUI or AvaloniaUI.**
> This is an independent project, maintained separately, and neither endorsed by nor supported by the ReactiveUI
> project, the AvaloniaUI project or the .NET Foundation. Parts of it are derived from ReactiveUI and
> ReactiveUI.Avalonia under the MIT licence — see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md). For the
> upstream libraries themselves, use [ReactiveUI](https://github.com/reactiveui/ReactiveUI) and
> [ReactiveUI.Avalonia](https://github.com/reactiveui/ReactiveUI.Avalonia). "ReactiveUI" and "Avalonia" are the
> marks of their respective projects; they are used here only to describe where this code came from.

## Why it exists

ReactiveUI's API is a good one, but the library carries a decade of platform support this project does not
need — WPF, WinForms, Xamarin, MAUI, a service locator, a reflection-based view locator. All of that costs
either trimming safety or dependencies.

Reactive.Avalonia keeps the API and drops the rest. It targets Avalonia and nothing else, depends only on
`Avalonia` and `System.Reactive`, and publishes clean under `PublishAot=true`.

## Getting started

Hook it into the application builder:

```csharp
using Reactive.Avalonia.Extensions;

public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
                 .UsePlatformDetect()
                 .UseReactive()
                 .LogToTrace();
```

### Properties fed by observables

```csharp
public sealed class SearchViewModel : ReactiveObject
{
    public SearchViewModel()
    {
        _summary = this.WhenAnyValue(x => x.Query)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .Select(query => $"Searching for {query}…")
            .ToProperty(this, x => x.Summary, out _summary);
    }

    public string Query
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    private readonly ObservableAsPropertyHelper<string> _summary;

    public string Summary => _summary.Value;
}
```

`ToProperty` marshals onto the UI thread for you, so the source sequence can run anywhere.

### Commands

```csharp
var canSave = this.WhenAnyValue(x => x.Query, query => !string.IsNullOrWhiteSpace(query));

Save = ReactiveCommand.CreateFromTask(SaveAsync, canSave);
Save.ThrownExceptions.Subscribe(ShowError);
```

A command is disabled while it is executing. `Execute()` is cold: the work starts when you subscribe, and
disposing that subscription cancels it — which is what makes the `CancellationToken` overloads mean something.
Binding a control to the command subscribes for you.

### Validation

Inherit `ReactiveValidationObject` and declare rules. Avalonia reads the resulting `INotifyDataErrorInfo`
itself, so a bound `TextBox` shows the message with no extra XAML.

```csharp
public sealed class SignUpViewModel : ReactiveValidationObject
{
    public SignUpViewModel()
    {
        this.ValidationRule(x => x.Email, email => email.Contains('@'), "Enter a valid email.");
        this.ValidationRule(
            x => x.Confirm,
            this.WhenAnyValue(x => x.Password, x => x.Confirm, (p, c) => p == c),
            "The passwords do not match.");

        SignUp = ReactiveCommand.CreateFromTask(SignUpAsync, this.IsValid());
    }
}
```

### Views and activation

```csharp
public partial class SearchView : ReactiveView<SearchViewModel>
{
    public SearchView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
            ViewModel!.Save.ThrownExceptions
                      .Subscribe(ShowError)
                      .DisposeWith(disposables));
    }
}
```

`ReactiveView<T>` (a `UserControl`) and `ReactiveWindow<T>` keep `ViewModel` and `DataContext` in step, so XAML
bindings and typed code-behind work at the same time. Everything registered inside `WhenActivated` is disposed
when the control leaves the visual tree and set up again if it returns. A view model implementing
`IActivatableViewModel` is activated alongside its view.

### Asking the view a question

```csharp
// view model
public Interaction<string, bool> Confirm { get; } = new();

private async Task DeleteAsync()
{
    if (await Confirm.Handle("Delete this file?"))
    {
        // …
    }
}

// view
this.WhenActivated(disposables =>
    ViewModel!.Confirm.RegisterHandler(async context =>
                  context.SetOutput(await ShowDialogAsync(context.Input)))
              .DisposeWith(disposables));
```

## Trimming and NativeAOT

The library is marked `IsTrimmable` and `IsAotCompatible`, and the sample publishes with zero ILLink warnings
under `PublishAot=true`. Two rules keep it that way, and they apply to your code too:

- Never call `Expression.Compile()`. Property lambdas here are *read* — the name and `PropertyInfo` are taken
  from the expression tree, which the compiler roots for the trimmer — never compiled.
- Avoid `Observable.FromEventPattern<TDelegate, TArgs>` and the `FromEvent` overloads without an explicit
  conversion delegate: Rx builds those handlers with `Expression.Compile()`. `FromEventPattern<TEventArgs>` and
  hand-written `Observable.Create` subscriptions are fine.

## What is deliberately missing

No Splat or service locator, no `ViewLocator`, no `RoutingState`/`RoutedViewHost`/`ViewModelViewHost`, no
`MessageBus`, no suspension host, and no code-behind `Bind()`/`BindCommand()` with its type-converter registry.
Use Avalonia's compiled XAML bindings and whichever DI container you already have.

## Differences from ReactiveUI

- Validation is built in; ReactiveUI needs the separate ReactiveUI.Validation package.
- `ObservableAsPropertyHelper` drops values equal to the current one instead of raising a redundant
  notification.
- `ReactiveObject` has no `ThrownExceptions`: an exception thrown by a `Changed` subscriber propagates out of
  the assignment that caused it rather than being routed to a side channel.
- Schedulers live on `RxSchedulers` and default to the Avalonia dispatcher without any initialisation call.
- `net10.0` only, with no polyfills.

## Building

```
dotnet build src/Reactive.Avalonia.slnx
dotnet test src/Reactive.Avalonia.slnx
```

`src/Reactive.Avalonia.Sample` is a runnable application covering each feature on its own tab, with desktop,
browser and Android heads. It is also published as an ahead-of-time compiled WebAssembly build on every push to `master`:
**[live demo](https://jackhammer88.github.io/Reactive.Avalonia/)**.

```
cd src/Reactive.Avalonia.Sample
dotnet run --project Reactive.Avalonia.Sample.Desktop
```

## Licence

MIT — see [LICENSE](LICENSE). Third-party attributions are in
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
