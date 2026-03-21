// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace LazyInterface;

/// <summary>
/// Represents a lazily evaluated value provider.
/// The value of type <typeparamref name="T"/> is computed only when requested,
/// and then can be cached by the implementation.
/// </summary>
/// <typeparam name="T">The type of the value to be lazily computed.</typeparam>
public interface ILazy<T>
{
    /// <summary>
    /// Returns the lazily evaluated value.
    /// Implementations ensure that the value is created on first access.
    /// </summary>
    /// <returns>The computed or cached value of type <typeparamref name="T"/>.</returns>
    T Get();
}