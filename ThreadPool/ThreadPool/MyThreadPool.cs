// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ThreadPool;

using System.Collections.Concurrent;

/// <summary>
/// A simple custom thread pool implementation that manages worker threads
/// and executes queued tasks.
/// </summary>
public class MyThreadPool
{
    private Thread[] threads = new Thread[1];
    private BlockingCollection<Action> taskQueue = new BlockingCollection<Action>();
    private CancellationTokenSource cts = new CancellationTokenSource();

    /// <summary>
    /// Initializes a new instance of the <see cref="MyThreadPool"/> class
    /// with the specified number of worker threads.
    /// </summary>
    /// <param name="n">The number of threads to create in the pool.</param>
    public MyThreadPool(int n)
    {
        this.threads = new Thread[n];
        for (int i = 0; i < n; i++)
        {
            this.threads[i] = new Thread(() =>
            {
                foreach (var task in this.taskQueue.GetConsumingEnumerable())
                {
                    if (this.cts.IsCancellationRequested)
                    {
                        return;
                    }

                    task();
                }
            });
            this.threads[i].Start();
        }
    }

    /// <summary>
    /// Gets a value indicating whether ThreadPool is shut down or not.
    /// </summary>
    public bool IsItOverForPool
    {
        get
        {
            if (this.cts.IsCancellationRequested)
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Adds a task to the thread pool’s work queue for execution.
    /// </summary>
    /// <typeparam name="TResult">The type of the result produced by the task.</typeparam>
    /// <param name="task">The task to be executed by the thread pool.</param>
    /// <returns>The same task instance that was added.</returns>
    /// <exception cref="InvalidOperationException">Thrown when attempting to queue a task after shutdown.</exception>
    public IMyTask<TResult> AddTask<TResult>(IMyTask<TResult> task)
    {
        ArgumentNullException.ThrowIfNull(task, "Null task");
        if (this.cts.IsCancellationRequested)
        {
            throw new InvalidOperationException("Cannot queue a task: the thread pool is shutting down.");
        }

        this.taskQueue.Add(task.Execute);
        return task;
    }

    /// <summary>
    /// Shuts down the thread pool gracefully, allowing current tasks to finish
    /// and preventing new ones from being queued.
    /// </summary>
    public void Shutdown()
    {
        this.cts.Cancel();
        this.taskQueue.CompleteAdding();
        foreach (var thread in this.threads)
        {
            thread.Join();
        }
    }
}
