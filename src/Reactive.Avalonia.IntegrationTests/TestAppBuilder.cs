using Avalonia;
using Avalonia.Headless;

using Reactive.Avalonia.Extensions;
using Reactive.Avalonia.Sample;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder
            .Configure<App>()
            .UseReactive()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}