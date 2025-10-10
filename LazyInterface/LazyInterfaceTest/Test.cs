// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LazyInterfaceTest;

using LazyInterface;

/// <summary>
/// Test class for SyncLazy and AsyncLazy classes.
/// </summary>
[TestClass]
public sealed class Test
{
    /// <summary>
    /// Verifies that <see cref="SyncLazy{T}"/> correctly computes and caches
    /// the value on first access, and that multiple instances using the same
    /// supplier function work independently.
    /// </summary>
    [TestMethod]
    public void TestSyncNormal()
    {
        Func<int> func = () => 2 + 2;
        SyncLazy<int> syncLazy1 = new SyncLazy<int>(func);
        SyncLazy<int> syncLazy2 = new SyncLazy<int>(func);
        SyncLazy<int> syncLazy3 = new SyncLazy<int>(() => 2 + 4);
        Assert.AreEqual(syncLazy1.Get(), 4);
        Assert.AreEqual(syncLazy1.Get(), 4);
        Assert.AreEqual(syncLazy2.Get(), 4);
        Assert.AreEqual(syncLazy2.Get(), 4);
        Assert.AreEqual(syncLazy3.Get(), 6);
        Assert.AreEqual(syncLazy3.Get(), 6);
    }

    /// <summary>
    /// Verifies that <see cref="SyncLazy{T}"/> correctly handles a supplier
    /// that returns <c>null</c> and does not throw an exception when the
    /// computed value itself is <c>null</c>.
    /// </summary>
    [ExpectedException(typeof(ArgumentNullException))]
    [TestMethod]
    public void TestNullValueSync()
    {
        Func<object?> func = () => null;
        #pragma warning disable CS8620
        SyncLazy<object> syncLazy1 = new SyncLazy<object>(func);
        #pragma warning restore CS8620
        Assert.IsNull(syncLazy1.Get());
    }

    /// <summary>
    /// Validates that <see cref="SyncLazy{T}"/> throws an
    /// <see cref="ArgumentNullException"/> when initialized with a <c>null</c> supplier function.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void TestNullFuncSync()
    {
        #pragma warning disable CS8625
        SyncLazy<object> syncLazy1 = new SyncLazy<object>(null);
        #pragma warning restore CS8625
        syncLazy1.Get();
    }

    /// <summary>
    /// Tests <see cref="AsyncLazy{T}"/> for correct behavior under concurrent access.
    /// Verifies that multiple threads obtain the same computed value, ensuring
    /// that lazy initialization occurs only once.
    /// </summary>
    [TestMethod]
    public void TestAsyncNormal()
    {
        Func<int> func = () =>
        {
            int i = 0;
            for (; i < 50; i++)
            {
            }

            return i;
        };

        AsyncLazy<int> asyncLazy1 = new AsyncLazy<int>(func);
        AsyncLazy<int> asyncLazy2 = new AsyncLazy<int>(func);
        int asyncLazy1result = 0;
        int asyncLazy2result = 0;

        for (int i = 0; i < 100; i++)
        {
            Thread thread1 = new Thread(() =>
            {
                asyncLazy1result = asyncLazy1.Get();
            });

            Thread thread2 = new Thread(() =>
            {
                asyncLazy2result = asyncLazy2.Get();
            });
            thread1.Start();
            thread2.Start();
            thread1.Join();
            thread2.Join();
            Assert.AreEqual(asyncLazy1result, asyncLazy2result);
        }
    }

    /// <summary>
    /// Validates that <see cref="AsyncLazy{T}"/> throws an
    /// <see cref="ArgumentNullException"/> when created with a <c>null</c> supplier function.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void TestNullFuncAsync()
    {
        #pragma warning disable CS8625
        AsyncLazy<object> asyncLazy1 = new AsyncLazy<object>(null);
        #pragma warning restore CS8625
        asyncLazy1.Get();
    }

    /// <summary>
    /// Verifies that <see cref="AsyncLazy{T}"/> properly throws an
    /// <see cref="ArgumentNullException"/> when the supplier function
    /// returns <c>null</c>, ensuring null results are handled as invalid.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void TestNullValueAsync()
    {
        Func<object?> func = () => null;
        #pragma warning disable CS8620
        AsyncLazy<object> asyncLazy1 = new AsyncLazy<object>(func);
        #pragma warning restore CS8620
        asyncLazy1.Get();
    }
}