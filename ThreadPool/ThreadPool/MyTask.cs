// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ThreadPool;

/// <summary>
/// Represents a single unit of work that can be executed by <see cref="MyThreadPool"/>.
/// Supports continuations and result retrieval.
/// </summary>
/// <typeparam name="TResult">The type of result produced by the task.</typeparam>
internal class MyTask<TResult> : IMyTask<TResult>
{
    private readonly Lock continuationsLock = new();

    private Func<TResult>? task;
    private MyThreadPool scheduler;
    private TResult? result;
    private Exception? capturedException;
    private ManualResetEventSlim gates = new();
    private int isStarted = 0;

    private List<Action>? continuations;

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
            this.gates.Wait();

            if (this.capturedException is not null)
            {
                throw new AggregateException(this.capturedException);
            }

            return this.result!;
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
        ArgumentNullException.ThrowIfNull(followingTask);

        var continuationTask = new MyTask<TNewResult>(() => followingTask(this.Result), this.scheduler);

        Action enqueue = () =>
        {
            try
            {
                this.scheduler.Enqueue(continuationTask);
            }
            catch (Exception ex)
            {
                continuationTask.Fail(ex);
            }
        };

        bool enqueueNow;
        lock (this.continuationsLock)
        {
            enqueueNow = this.IsCompleted;
            if (!enqueueNow)
            {
                this.continuations ??= new List<Action>();
                this.continuations.Add(enqueue);
            }
        }

        if (enqueueNow)
        {
            enqueue();
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
            this.result = this.task!();
        }
        catch (Exception ex)
        {
            this.capturedException = ex;
        }
        finally
        {
            this.gates.Set();
            this.RunContinuations();
        }

        this.task = null;
    }

    /// <summary>
    /// Releases all resources used by the task.
    /// </summary>
    public void Dispose() => this.gates.Dispose();

    /// <summary>
    /// Marks the task as completed with an error and releases any waiters.
    /// </summary>
    /// <param name="ex">The exception that caused the task to fail.</param>
    internal void Fail(Exception ex)
    {
        if (Interlocked.Exchange(ref this.isStarted, 1) == 1)
        {
            return;
        }

        this.capturedException = ex;
        this.gates.Set();
        this.RunContinuations();
    }

    private void RunContinuations()
    {
        List<Action>? toRun = null;

        lock (this.continuationsLock)
        {
            toRun = this.continuations;
            this.continuations = null;
        }

        if (toRun is null)
        {
            return;
        }

        foreach (var action in toRun)
        {
            try
            {
                action();
            }
            catch
            {
            }
        }
    }
}
