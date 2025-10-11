// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using LazyInterface;

/// <summary>
/// Provides single-threaded lazy initialization: the supplier runs once on the first <see cref="Get"/> call,
/// its result is cached, and subsequent calls return the cached value. The supplier may return <c>null</c>.
/// </summary>
/// <typeparam name="T">The type of the value produced by the supplier.</typeparam>
public class SyncLazy<T> : ILazy<T>
{
    private T? value;
    private Func<T>? supplier;
    private bool isExecuted = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncLazy{T}"/> class
    /// with the specified supplier function.
    /// </summary>
    /// <param name="supply">
    /// A function that computes and returns the value when it is first requested.
    /// </param>
    public SyncLazy(Func<T> supply)
    {
        this.supplier = supply;
    }

    /// <summary>
    /// Returns the lazily computed value. On first invocation, executes the supplier, caches its result,
    /// and releases the supplier reference; later calls return the cached value.
    /// </summary>
    /// <returns>The cached value; may be <c>null</c> when <typeparamref name="T"/> allows it.</returns>
    public T Get()
    {
        if (!this.isExecuted)
        {
            ArgumentNullException.ThrowIfNull(this.supplier, "Null supplier");
            this.value = this.supplier();
            this.isExecuted = true;
            this.supplier = null;
        }

        ArgumentNullException.ThrowIfNull(this.value, "Null value");
        return this.value;
    }
}