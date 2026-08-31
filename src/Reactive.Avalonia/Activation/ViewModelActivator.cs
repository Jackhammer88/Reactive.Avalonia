// Derived from ReactiveUI 23.2.28 (ViewModelActivator).
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT license.
// See THIRD-PARTY-NOTICES.md in the repository root.

using System.Threading;

namespace Reactive.Avalonia;

/// <summary>
/// Tracks whether a view model is currently being shown, and runs its activation blocks accordingly.
/// </summary>
/// <remarks>
/// Activation is reference counted, so a view model shown in two places at once activates once and deactivates
/// when the last view goes away.
/// </remarks>
public sealed class ViewModelActivator : IDisposable
{
    private readonly List<Func<IEnumerable<IDisposable>>> _blocks = [];
    private readonly Subject<Unit> _activated = new();
    private readonly Subject<Unit> _deactivated = new();
    private readonly Lock _gate = new();
    private CompositeDisposable _handle = [];
    private int _refCount;

    /// <summary>
    /// Gets a sequence that ticks each time the view model becomes active.
    /// </summary>
    public IObservable<Unit> Activated => _activated.AsObservable();

    /// <summary>
    /// Gets a sequence that ticks each time the view model becomes inactive.
    /// </summary>
    public IObservable<Unit> Deactivated => _deactivated.AsObservable();

    /// <summary>
    /// Marks the view model as active, running its activation blocks if this is the first activation.
    /// </summary>
    /// <returns>A token that deactivates when disposed.</returns>
    public IDisposable Activate()
    {
        lock (_gate)
        {
            if (++_refCount == 1)
            {
                RunBlocks();
                _activated.OnNext(Unit.Default);
            }
        }

        return Disposable.Create(this, static activator => activator.Deactivate());
    }

    /// <summary>
    /// Drops one activation reference, tearing down the activation blocks when the last one goes.
    /// </summary>
    /// <param name="ignoreRefCount">
    /// When <see langword="true"/>, deactivates immediately no matter how many views are still showing the
    /// view model. Use this only when tearing an application down.
    /// </param>
    public void Deactivate(bool ignoreRefCount = false)
    {
        lock (_gate)
        {
            if (!ignoreRefCount)
            {
                // Deactivating something that was never activated is a no-op, not an underflow.
                if (_refCount == 0 || --_refCount > 0)
                {
                    return;
                }
            }

            _refCount = 0;
            _handle.Dispose();
            _handle = [];
            _deactivated.OnNext(Unit.Default);
        }
    }

    /// <summary>
    /// Releases the activator, deactivating first if needed.
    /// </summary>
    public void Dispose()
    {
        lock (_gate)
        {
            _blocks.Clear();
            _handle.Dispose();
            _handle = [];
            _refCount = 0;
        }

        _activated.Dispose();
        _deactivated.Dispose();
    }

    /// <summary>
    /// Registers a block to run on every activation.
    /// </summary>
    /// <param name="block">Produces the subscriptions to dispose on deactivation.</param>
    /// <returns>A token that unregisters the block.</returns>
    internal IDisposable AddActivationBlock(Func<IEnumerable<IDisposable>> block)
    {
        lock (_gate)
        {
            _blocks.Add(block);

            // Registering while already active should not wait for the next activation to take effect.
            if (_refCount > 0)
            {
                AddRange(_handle, block());
            }
        }

        return Disposable.Create(
            (self: this, block),
            static state =>
            {
                lock (state.self._gate)
                {
                    state.self._blocks.Remove(state.block);
                }
            });
    }

    private static void AddRange(CompositeDisposable target, IEnumerable<IDisposable> items)
    {
        foreach (var item in items)
        {
            target.Add(item);
        }
    }

    private void RunBlocks()
    {
        foreach (var block in _blocks)
        {
            AddRange(_handle, block());
        }
    }
}