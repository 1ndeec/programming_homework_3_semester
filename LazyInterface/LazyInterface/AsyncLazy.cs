// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LazyInterface;

/// <summary>
/// A thread-safe implementation of the <see cref="ILazy{T}"/> interface.
/// Ensures that the value is computed only once, even when accessed
/// concurrently from multiple threads.
/// </summary>
/// <typeparam name="T">The type of the value to be lazily initialized.</typeparam>
public class AsyncLazy<T> : ILazy<T>
{
    private T? value;
    private Func<T>? supplier;
    private volatile bool isExecuted = false;
    private object lockHolder = new object();

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncLazy{T}"/> class
    /// with the specified supplier function.
    /// </summary>
    /// <param name="supply">
    /// A function that computes and returns the value when it is first requested.
    /// </param>
    public AsyncLazy(Func<T> supply)
    {
        this.supplier = supply;
    }

    /// <summary>
    /// Returns the lazily computed value.
    /// If multiple threads call this method simultaneously,
    /// only one will execute the supplier function;
    /// others will wait until the result is ready.
    /// </summary>
    /// <returns>The computed or cached value of type <typeparamref name="T"/>.</returns>
    public T Get()
    {
        lock (this.lockHolder)
        {
            if (!this.isExecuted)
            {
                ArgumentNullException.ThrowIfNull(this.supplier, "Null supplier");
                this.value = this.supplier();
                this.supplier = null;
                this.isExecuted = true;
            }

            ArgumentNullException.ThrowIfNull(this.value, "Null value");
            return this.value;
        }
    }
}