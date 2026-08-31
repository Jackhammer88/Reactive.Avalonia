# Third-party notices

Reactive.Avalonia is not a clean-room implementation. Parts of it are derived from the projects below, all of
which are MIT licensed. Their copyright notices and the MIT permission notice are reproduced here as those
licenses require.

---

## ReactiveUI

Copyright (c) .NET Foundation and Contributors

<https://github.com/reactiveui/ReactiveUI> — MIT.

The public API of this library follows ReactiveUI's, and the following are derived from ReactiveUI 23.2.28:

- `ReactiveObject`, `IReactiveObject`, `ReactiveObjectExtensions` — `RaiseAndSetIfChanged`,
  `SuppressChangeNotifications` and `DelayChangeNotifications` semantics.
- `ObservableAsPropertyHelper<T>` and the `ToProperty` overloads.
- `WhenAnyValue`, `WhenAnyObservable`, `ObservableForProperty`, `IObservedChange<TSender, TValue>`.
- `ReactiveCommand`, `IReactiveCommand`, `InvokeCommand`.
- `ViewModelActivator`, `IActivatableViewModel`, `IActivatableView`, `WhenActivated`.
- `Interaction<TInput, TOutput>` and `InteractionContext<TInput, TOutput>`, including the wording of the
  "Output has not been set." and "Output has already been set." messages.
- `ScheduledSubject<T>`, `RxSchedulers`, `UnhandledErrorException`.
- `IViewFor` and `IViewFor<TViewModel>`.
- Most of the tests in `Reactive.Avalonia.Tests`, adapted from `ReactiveUI.Tests` (TUnit to NUnit). Each such
  file names the test class it came from.

## ReactiveUI.Avalonia

Copyright (c) 2019-2026 ReactiveUI and Avalonia Teams, and Contributors

<https://github.com/reactiveui/ReactiveUI.Avalonia> — MIT.

Derived from version 11.4.13, the last release built against System.Reactive:

- `AvaloniaScheduler` — a close port, including the reentrancy cap that keeps inlined scheduling from
  overflowing the stack.
- `ReactiveWindow<TViewModel>`, `ReactiveView<TViewModel>` and the `ViewModel`/`DataContext` synchronisation in
  `ViewModelBinding`.

## Avalonia

Copyright (c) .NET Foundation and Contributors

<https://github.com/AvaloniaUI/Avalonia> — MIT.

`Reactive.Avalonia.Sample` started from the Avalonia MVVM project template (`Program.cs`, `App.axaml`,
`App.axaml.cs`, `app.manifest`, `Assets/avalonia-logo.ico`), and the headless test scaffolding in
`Reactive.Avalonia.IntegrationTests` (`TestAppBuilder.cs`, `Tests1.cs`) came from the Avalonia headless testing
template. Avalonia is also redistributed inside any self-contained or NativeAOT build of the sample.

## System.Reactive

Copyright (c) .NET Foundation and Contributors

<https://github.com/dotnet/reactive> — MIT.

Referenced as a NuGet package, and redistributed inside any self-contained or NativeAOT build.

---

## MIT License

The permission notice below applies to each of the projects listed above.

```
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
