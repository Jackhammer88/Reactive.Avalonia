using System.Threading.Tasks;

using Avalonia;
using Avalonia.Browser;

using Reactive.Avalonia.Extensions;

namespace Reactive.Avalonia.Sample.Browser;

internal sealed partial class Program
{
    private static Task Main() => BuildAvaloniaApp()
        .StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .WithInterFont()
#if DEBUG
            .WithDeveloperTools()
#endif
            .UseReactive();
}