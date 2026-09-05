using Avalonia;

namespace Reactive.Avalonia.Extensions;

/// <summary>
/// Hooks Reactive.Avalonia into the Avalonia application builder.
/// </summary>
public static class AppBuilderExtension
{
    /// <summary>
    /// Configures the application to use Reactive.Avalonia.
    /// </summary>
    /// <param name="appBuilder">The application builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <example>
    /// <code language="csharp">
    /// public static AppBuilder BuildAvaloniaApp()
    ///     => AppBuilder.Configure&lt;App&gt;()
    ///                  .UsePlatformDetect()
    ///                  .UseReactive()
    ///                  .LogToTrace();
    /// </code>
    /// </example>
    public static AppBuilder UseReactive(this AppBuilder appBuilder) => appBuilder.UseReactive(null);

    /// <summary>
    /// Configures the application to use Reactive.Avalonia, overriding the defaults.
    /// </summary>
    /// <param name="appBuilder">The application builder.</param>
    /// <param name="configure">Adjusts the schedulers and error handling.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <remarks>
    /// Configuration is applied after Avalonia's platform services are up, which is the first point at which
    /// the dispatcher is guaranteed to exist.
    /// </remarks>
    public static AppBuilder UseReactive(this AppBuilder appBuilder, Action<ReactiveOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(appBuilder);

        return appBuilder.AfterPlatformServicesSetup(_ =>
        {
            var options = new ReactiveOptions();
            configure?.Invoke(options);

            RxSchedulers.MainThreadScheduler = options.MainThreadScheduler;
            RxSchedulers.TaskpoolScheduler = options.TaskpoolScheduler;

            if (options.DefaultExceptionHandler is not null)
                RxSchedulers.DefaultExceptionHandler = options.DefaultExceptionHandler;
        });
    }
}