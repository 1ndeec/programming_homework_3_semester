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
                        try
                        {
                            var t = pool.AddTask(() => Interlocked.Increment(ref executed));
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

            var parent = pool.AddTask(() =>
            {
                releaseParent.Wait();
                return 1;
            });

            var continuation = parent.ContinueWith(x => x + 1);

            var shutdown = Task.Run(() => pool.Shutdown());

            Assert.IsTrue(SpinWait.SpinUntil(() => pool.IsItOverForPool, 2000), "Pool did not enter shutdown state in time.");

            releaseParent.Set();

            shutdown.Wait();

            var contGetter = Task.Run(() =>
            {
                try
                {
                    _ = continuation.Result;
                }
                catch
                {
                }
            });

            Assert.IsTrue(contGetter.Wait(3000), "Continuation must not hang on Result.");
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

        Assert.Throws<InvalidOperationException>(() => pool.AddTask(() => 1));
    }

    /// <summary>
    /// Verifies that an exception thrown inside a task is propagated through Result.
    /// </summary>
    [TestMethod]
    public void Task_WhenThrows_ResultRethrows()
    {
        var pool = new MyThreadPool(1);

        try
        {
            var t = pool.AddTask<int>(() => throw new InvalidOperationException("boom"));

            Assert.Throws<AggregateException>(() =>
            {
                _ = t.Result;
            });
        }
        finally
        {
            pool.Shutdown();
        }
    }
}