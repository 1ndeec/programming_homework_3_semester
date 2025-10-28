// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ThreadPoolTest;

using ThreadPool;

/// <summary>
/// Contains unit tests for MyThreadPool and MyTask functionality.
/// </summary>
[TestClass]
public sealed class TestWithMyTask
{
    /// <summary>
    /// Checks that a single task runs correctly and returns the expected result.
    /// </summary>
    [TestMethod]
    public void AddTask_Executes_AndReturnsResult()
    {
        var pool = new MyThreadPool(2);
        var task = new MyTask<int>(() => 1 + 2, pool);

        var returned = pool.AddTask(task);
        Assert.AreEqual(task, returned);

        var value = returned.Result;
        Assert.AreEqual(3, value);
        Assert.IsTrue(returned.IsCompleted);
    }

    /// <summary>
    /// Verifies that multiple tasks can run and return their results successfully.
    /// </summary>
    [TestMethod]
    public void AddTask_MultipleTasks_AllRun()
    {
        var pool = new MyThreadPool(3);

        var t1 = new MyTask<int>(() => 10, pool);
        var t2 = new MyTask<string>(() => "ok", pool);
        var t3 = new MyTask<bool>(() => true, pool);

        pool.AddTask(t1);
        pool.AddTask(t2);
        pool.AddTask(t3);

        Assert.AreEqual(10, t1.Result);
        Assert.AreEqual("ok", t2.Result);
        Assert.IsTrue(t3.Result);
    }

    /// <summary>
    /// Ensures that task exceptions are wrapped and rethrown as AggregateException.
    /// </summary>
    [TestMethod]
    public void Task_Exception_Propagates_AsAggregateException()
    {
        var pool = new MyThreadPool(1);
        var boom = new InvalidOperationException("boom!");
        var t = new MyTask<int>(() => throw boom, pool);

        pool.AddTask(t);

        var ex = Assert.Throws<AggregateException>(() => { var temp = t.Result; });
        Assert.AreSame(boom, ex.InnerException);
        Assert.IsTrue(t.IsCompleted);
    }

    /// <summary>
    /// Checks that adding a task after shutdown throws an exception.
    /// </summary>
    [TestMethod]
    public void Shutdown_Then_AddTask_Throws()
    {
        var pool = new MyThreadPool(1);
        pool.Shutdown();

        var t = new MyTask<int>(() => 42, pool);
        var thrown = Assert.Throws<InvalidOperationException>(() => pool.AddTask(t));
        StringAssert.Contains(thrown.Message, "shutting down");
    }

    /// <summary>
    /// Confirms that tasks already running finish after shutdown, but new ones are rejected.
    /// </summary>
    [TestMethod]
    public void Shutdown_Allows_InFlight_ToFinish_But_Rejects_New()
    {
        var pool = new MyThreadPool(1);

        var t1 = new MyTask<int>(
            () =>
            {
                Thread.Sleep(10000);
                return 7;
            },
            pool);

        pool.AddTask(t1);

        // let threadpool catch the task
        Thread.Sleep(75);

        pool.Shutdown();

        Assert.AreEqual(7, t1.Result);

        var t2 = new MyTask<int>(() => 8, pool);
        Assert.Throws<InvalidOperationException>(() => pool.AddTask(t2));
    }
}