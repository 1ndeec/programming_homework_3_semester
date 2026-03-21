// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LazyInterfaceTest;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LazyInterface;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for <see cref="ILazy{T}"/> implementations.
/// Includes shared behavior tests for both implementations and a dedicated concurrency test for <see cref="ParalellLazy{T}"/>.
/// </summary>
[TestClass]
public sealed class Test
{
    /// <summary>
    /// Provides factories for creating different <see cref="ILazy{T}"/> implementations to be used in data-driven tests.
    /// </summary>
    /// <returns>
    /// A sequence of test cases where each case contains:
    /// a display name and a factory function that constructs an <see cref="ILazy{T}"/> from a supplier.
    /// </returns>
    public static IEnumerable<object[]> LazyFactories()
    {
        yield return new object[]
        {
            new Func<Func<object?>, ILazy<object?>>(s => new SyncLazy<object?>(s!)),
        };

        yield return new object[]
        {
            new Func<Func<object?>, ILazy<object?>>(s => new ParalellLazy<object?>(s!)),
        };
    }

    /// <summary>
    /// Verifies that calling <c>Get()</c> multiple times returns the same cached value
    /// and that the supplier is executed exactly once.
    /// </summary>
    /// <param name="factory">A factory that creates an <see cref="ILazy{T}"/> instance from a supplier.</param>
    [DataTestMethod]
    [DynamicData(nameof(LazyFactories), DynamicDataSourceType.Method)]
    public void SupplierIsCalledOnce_OnMultipleGets(Func<Func<object?>, ILazy<object?>> factory)
    {
        int calls = 0;
        Func<object?> supplier = () =>
        {
            Interlocked.Increment(ref calls);
            return 123;
        };

        var lazy = factory(supplier);

        var a = lazy.Get();
        var b = lazy.Get();
        var c = lazy.Get();

        Assert.AreEqual(123, a);
        Assert.AreEqual(123, b);
        Assert.AreEqual(123, c);
        Assert.AreEqual(1, calls);
    }

    /// <summary>
    /// Verifies that passing a <c>null</c> supplier to the constructor throws <see cref="ArgumentNullException"/>.
    /// </summary>
    /// <param name="factory">A factory that creates an <see cref="ILazy{T}"/> instance from a supplier.</param>
    [DataTestMethod]
    [DynamicData(nameof(LazyFactories), DynamicDataSourceType.Method)]
    public void Constructor_Throws_OnNullSupplier(Func<Func<object?>, ILazy<object?>> factory)
    {
        Assert.ThrowsException<ArgumentNullException>(() => factory(null!));
    }

    /// <summary>
    /// Verifies thread-safety of <see cref="ParalellLazy{T}"/>:
    /// when many threads call <c>Get()</c> concurrently on the same instance,
    /// the supplier is executed exactly once and all threads observe the same cached value.
    /// </summary>
    [TestMethod]
    public void ParalellLazy_SupplierRunsOnce_WhenGetCalledConcurrently()
    {
        int calls = 0;

        var lazy = new ParalellLazy<int>(() => Interlocked.Increment(ref calls));

        const int workers = 20;
        var startBarrier = new Barrier(workers + 1);

        var tasks = new Task<int>[workers];
        for (int i = 0; i < workers; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                startBarrier.SignalAndWait();
                return lazy.Get();
            });
        }

        startBarrier.SignalAndWait();
        Task.WaitAll(tasks);

        Assert.AreEqual(1, calls, "Supplier must be called exactly once.");
        Assert.IsTrue(tasks.All(t => t.Result == 1), "All threads must see the same cached value.");
    }
}