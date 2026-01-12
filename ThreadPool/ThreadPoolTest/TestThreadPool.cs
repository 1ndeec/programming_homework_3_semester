// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ThreadPoolTest;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThreadPool;

/// <summary>
/// Contains race-condition and correctness tests for <see cref="MyThreadPool"/> and tasks executed within it.
/// </summary>
[TestClass]
public sealed class TestThreadPool
{
    /// <summary>
    /// Verifies that the pool creates exactly the number of worker threads requested in the constructor.
    /// </summary>
    [TestMethod]
    public void ThreadCount_MatchesConstructorArgument()
    {
        var pool = new MyThreadPool(3);

        try
        {
            var threads = GetWorkerThreads(pool);
            Assert.AreEqual(3, threads.Length);
        }
        finally
        {
            pool.Shutdown();
        }
    }

    /// <summary>
    /// Verifies that concurrent calls to <c>AddTask</c> racing with <c>Shutdown</c> do not lead to "accepted but never executed" tasks.
    /// If a task is accepted by <c>AddTask</c>, it must eventually complete and produce a result.
    /// </summary>
    [TestMethod]
    [Timeout(15000, CooperativeCancellation = true)]
    public void AddTask_RacingWithShutdown_NoAcceptedTaskHangs()
    {
        var pool = new MyThreadPool(4);

        try
        {
            const int producers = 8;
            const int tasksPerProducer = 50;
            var start = new Barrier(producers + 1);

            var accepted = new ConcurrentBag<IMyTask<int>>();
            int executed = 0;

            Task[] producerTasks = new Task[producers];
            for (int p = 0; p < producers; p++)
            {
                producerTasks[p] = Task.Run(() =>
                {
                    start.SignalAndWait();

                    for (int i = 0; i < tasksPerProducer; i++)
                    {
                        var t = CreateMyTask(() => Interlocked.Increment(ref executed), pool);

                        try
                        {
                            pool.AddTask(t);
                            accepted.Add(t);
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    }
                });
            }

            var shutdownTask = Task.Run(() =>
            {
                start.SignalAndWait();
                pool.Shutdown();
            });

            Task.WaitAll(producerTasks);
            shutdownTask.Wait();

            foreach (var t in accepted)
            {
                var getter = Task.Run(() => t.Result);
                Assert.IsTrue(getter.Wait(3000), "An accepted task must not hang on Result.");
            }

            Assert.AreEqual(accepted.Count, executed, "Every accepted task must be executed exactly once.");
        }
        finally
        {
            if (!pool.IsItOverForPool)
            {
                pool.Shutdown();
            }
        }
    }

    /// <summary>
    /// Verifies that a continuation created via <c>ContinueWith</c> does not deadlock even if the pool shuts down
    /// between the continuation registration and the parent task completion.
    /// </summary>
    [TestMethod]
    [Timeout(15000, CooperativeCancellation = true)]
    public void ContinueWith_RacingWithShutdown_ContinuationDoesNotDeadlock()
    {
        var pool = new MyThreadPool(2);

        try
        {
            var releaseParent = new ManualResetEventSlim(false);

            var parent = CreateMyTask(
                () =>
            {
                releaseParent.Wait();
                return 1;
            },
                pool);

            pool.AddTask(parent);

            var continuation = parent.ContinueWith(x => x + 1);

            var shutdown = Task.Run(() => pool.Shutdown());

            Assert.IsTrue(SpinWait.SpinUntil(() => pool.IsItOverForPool, 2000), "Pool did not enter shutdown state in time.");

            releaseParent.Set();

            shutdown.Wait();

            var contGetter = Task.Run(() => continuation.Result);
            Assert.IsTrue(contGetter.Wait(3000), "Continuation must not hang on Result.");

            if (contGetter.IsFaulted)
            {
                Assert.IsNotNull(contGetter.Exception);
            }
        }
        finally
        {
            if (!pool.IsItOverForPool)
            {
                pool.Shutdown();
            }
        }
    }

    /// <summary>
    /// Verifies that adding a task after shutdown is rejected with <see cref="InvalidOperationException"/>.
    /// </summary>
    [TestMethod]
    public void AddTask_AfterShutdown_Throws()
    {
        var pool = new MyThreadPool(1);

        pool.Shutdown();

        var t = CreateMyTask(() => 1, pool);
        Assert.Throws<InvalidOperationException>(() => pool.AddTask(t));
    }

    private static Thread[] GetWorkerThreads(MyThreadPool pool)
    {
        var f = typeof(MyThreadPool).GetField("threads", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(f);

        var value = f.GetValue(pool);
        Assert.IsNotNull(value);

        return (Thread[])value;
    }

    private static IMyTask<TResult> CreateMyTask<TResult>(Func<TResult> func, MyThreadPool pool)
    {
        var asm = typeof(MyThreadPool).Assembly;
        var open = asm.GetType("ThreadPool.MyTask`1", throwOnError: true);
        var closed = open!.MakeGenericType(typeof(TResult));

        var obj = Activator.CreateInstance(closed, func, pool);
        Assert.IsNotNull(obj);

        return (IMyTask<TResult>)obj;
    }
}