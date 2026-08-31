// Derived from ReactiveUI 23.2.28 (InteractionContext).
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT license.
// See THIRD-PARTY-NOTICES.md in the repository root.

namespace Reactive.Avalonia;

/// <summary>
/// The request/response pair passed to an <see cref="Interaction{TInput, TOutput}"/> handler.
/// </summary>
/// <typeparam name="TInput">What the view model is asking about.</typeparam>
/// <typeparam name="TOutput">What it needs back.</typeparam>
public sealed class InteractionContext<TInput, TOutput>
{
    private TOutput _output = default!;

    /// <summary>
    /// Initializes a new instance of the <see cref="InteractionContext{TInput, TOutput}"/> class.
    /// </summary>
    /// <param name="input">The value being asked about.</param>
    internal InteractionContext(TInput input) => Input = input;

    /// <summary>
    /// Gets the value the view model is asking about.
    /// </summary>
    public TInput Input { get; }

    /// <summary>
    /// Gets a value indicating whether a handler has answered.
    /// </summary>
    public bool IsHandled { get; private set; }

    /// <summary>
    /// Answers the interaction. Handlers registered earlier are not consulted.
    /// </summary>
    /// <param name="output">The answer.</param>
    /// <exception cref="InvalidOperationException">The interaction was already answered.</exception>
    /// <remarks>
    /// Answering twice is a bug rather than a last-one-wins convenience: it means two handlers both believed
    /// they owned the interaction.
    /// </remarks>
    public void SetOutput(TOutput output)
    {
        if (IsHandled)
        {
            throw new InvalidOperationException("Output has already been set.");
        }

        _output = output;
        IsHandled = true;
    }

    /// <summary>
    /// Reads the answer.
    /// </summary>
    /// <returns>The value passed to <see cref="SetOutput"/>.</returns>
    /// <exception cref="InvalidOperationException">No handler answered.</exception>
    public TOutput GetOutput() =>
        IsHandled ? _output : throw new InvalidOperationException("Output has not been set.");
}