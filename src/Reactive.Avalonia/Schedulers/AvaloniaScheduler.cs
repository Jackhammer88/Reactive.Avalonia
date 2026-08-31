// Ported from ReactiveUI.Avalonia 11.4.13 (AvaloniaScheduler).
// Copyright (c) 2019-2026 ReactiveUI and Avalonia Teams, and Contributors. Licensed under the MIT license.
// See THIRD-PARTY-NOTICES.md in the repository root.

using System.Threading;

using Avalonia.Threading;

namespace Reactive.Avalonia;

/// <summary>
/// An <see cref="IScheduler"/> that runs work on the Avalonia UI thread.
/// </summary>
/// <remarks>
/// Work scheduled with no delay from the UI thread runs inline, which keeps reactive chains cheap. Inlining is
/// capped at <see cref="MaxReentrantSchedules"/> levels so a recursive chain degrades into dispatcher posts
/// instead of overflowing the stack.
/// </remarks>
public sealed class AvaloniaScheduler : LocalScheduler
{
    /// <summary>
    /// The scheduler bound to <see cref="Dispatcher.UIThread"/>.
    /// </summary>
    public static readonly AvaloniaScheduler Instance = new(DispatcherPriority.Background);

    private const int MaxReentrantSchedules = 32;

    private readonly DispatcherPriority _priority;

    [ThreadStatic]
    private static int _reentrancyGuard;

    private AvaloniaScheduler(DispatcherPriority priority) => _priority = priority;

    /// <summary>
    /// Creates a scheduler that posts to the UI thread at the given dispatcher priority.
    /// </summary>
    /// <param name="priority">The dispatcher priority to post at.</param>
    /// <returns>A scheduler using <paramref name="priority"/>.</returns>
    public static AvaloniaScheduler With(DispatcherPriority priority) => new(priority);

    /// <inheritdoc/>
    public override IDisposable Schedule<TState>(
        TState state,
        TimeSpan dueTime,
        Func<IScheduler, TState, IDisposable> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (dueTime > TimeSpan.Zero)
        {
            var delayed = new CompositeDisposable(2);
            delayed.Add(DispatcherTimer.RunOnce(() => delayed.Add(action(this, state)), dueTime, _priority));
            return delayed;
        }

        if (!Dispatcher.UIThread.CheckAccess() || _reentrancyGuard >= MaxReentrantSchedules)
        {
            return Post(state, action);
        }

        try
        {
            _reentrancyGuard++;
            return action(this, state);
        }
        finally
        {
            _reentrancyGuard--;
        }
    }

    private IDisposable Post<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
    {
        var composite = new CompositeDisposable(2);
        var cancellation = new CancellationDisposable();

        Dispatcher.UIThread.Post(
            () =>
            {
                if (!cancellation.Token.IsCancellationRequested)
                {
                    composite.Add(action(this, state));
                }
            },
            _priority);

        composite.Add(cancellation);
        return composite;
    }
}