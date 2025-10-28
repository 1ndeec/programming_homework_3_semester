// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ThreadPool;

/// <summary>
/// Represents a generic task that can be executed within a custom thread pool.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the task.</typeparam>
public interface IMyTask<TResult>
{
    /// <summary>
    /// Gets a value indicating whether the task has completed execution.
    /// </summary>
    public bool IsCompleted { get; }

    /// <summary>
    /// Gets the result of the task after completion.
    /// </summary>
    public TResult Result { get; }

    /// <summary>
    /// Creates a continuation task that runs after this task completes.
    /// </summary>
    /// <typeparam name="TNewResult">The result type of the continuation task.</typeparam>
    /// <param name="followingTask">A function to execute after the current task finishes, using its result as input.</param>
    /// <returns>A new task representing the continuation.</returns>
    IMyTask<TNewResult> ContinueWith<TNewResult>(Func<TResult, TNewResult> followingTask);

    /// <summary>
    /// Executes the task’s assigned action.
    /// </summary>
    public void Execute();
}