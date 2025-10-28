// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ThreadPool;

/// <summary>
/// Represents a single unit of work that can be executed by <see cref="MyThreadPool"/>.
/// Supports continuations and result retrieval.
/// </summary>
/// <typeparam name="TResult">The type of result produced by the task.</typeparam>
public class MyTask<TResult> : IMyTask<TResult>
{
    private Func<TResult> task;
    private MyThreadPool scheduler;
    private TResult? result;
    private Exception capturedException = new Exception("Placeholder");
    private ManualResetEventSlim gates = new ManualResetEventSlim();
    private int isStarted = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="MyTask{TResult}"/> class with a given function and thread pool.
    /// </summary>
    /// <param name="func">The function to execute as the task.</param>
    /// <param name="scheduler">The thread pool responsible for executing the task.</param>
    public MyTask(Func<TResult> func, MyThreadPool scheduler)
    {
        ArgumentNullException.ThrowIfNull(func, "Null func");
        ArgumentNullException.ThrowIfNull(scheduler, "Null parent threadpool");
        this.task = func;
        this.scheduler = scheduler;
    }

    /// <summary>
    /// Gets a value indicating whether the task has finished executing.
    /// </summary>
    public bool IsCompleted => this.gates.IsSet;

    /// <summary>
    /// Gets the result of the task, blocking until the task is completed if necessary.
    /// Throws <see cref="AggregateException"/> if the task failed.
    /// </summary>
    public TResult Result
    {
        get
        {
            if (this.scheduler.IsItOverForPool && this.isStarted == 0)
            {
                throw new InvalidOperationException("Parent thread pool shut down.");
            }

            if (!this.gates.IsSet)
            {
                this.gates.Wait();
            }

            if (this.capturedException.Message != "Placeholder")
            {
                throw new AggregateException(this.capturedException);
            }

            if (this.result != null)
            {
                return this.result;
            }
            else
            {
                throw new InvalidOperationException("Method attempted to return a null value.");
            }
        }
    }

    /// <summary>
    /// Creates a continuation task that runs after this task completes.
    /// </summary>
    /// <typeparam name="TNewResult">The type of result for the continuation task.</typeparam>
    /// <param name="followingTask">The function to execute after the current task finishes.</param>
    /// <returns>A new task representing the continuation.</returns>
    public IMyTask<TNewResult> ContinueWith<TNewResult>(Func<TResult, TNewResult> followingTask)
    {
        Func<TNewResult> loadedFollowingTask = () => followingTask(this.Result);
        MyTask<TNewResult> continuationTask = new MyTask<TNewResult>(loadedFollowingTask, this.scheduler);
        if (this.IsCompleted)
        {
            this.scheduler.AddTask(continuationTask);
        }

        return continuationTask;
    }

    /// <summary>
    /// Executes the assigned function and stores its result or exception.
    /// </summary>
    public void Execute()
    {
        if (Interlocked.Exchange(ref this.isStarted, 1) == 1)
        {
            return;
        }

        try
        {
            this.result = this.task();
        }
        catch (Exception ex)
        {
            this.capturedException = ex;
        }
        finally
        {
            this.gates.Set();
        }
    }

    /// <summary>
    /// Releases all resources used by the task.
    /// </summary>
    public void Dispose() => this.gates.Dispose();
}
