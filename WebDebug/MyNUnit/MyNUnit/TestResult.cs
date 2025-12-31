// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MyNUnit;

using System;
using System.Reflection;

/// <summary>
/// Represents the outcome of a test.
/// </summary>
public enum TestStatus
{
    /// <summary>
    /// The test completed without errors.
    /// </summary>
    Passed,

    /// <summary>
    /// The test threw an unexpected exception.
    /// </summary>
    Failed,

    /// <summary>
    /// The test was skipped.
    /// </summary>
    Skipped,
}

/// <summary>
/// Stores information about an executed test.
/// </summary>
public sealed class TestResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestResult"/> class.
    /// </summary>
    /// <param name="method">The executed method.</param>
    /// <param name="status">The test outcome.</param>
    /// <param name="duration">Execution time.</param>
    /// <param name="message">Optional message.</param>
    /// <param name="exception">Exception thrown by the test.</param>
    public TestResult(MethodInfo method, TestStatus status, TimeSpan duration, string? message = null, Exception? exception = null)
    {
        this.Method = method;
        this.Status = status;
        this.Duration = duration;
        this.Message = message;
        this.Exception = exception;
    }

    /// <summary>
    /// Gets the method that was executed.
    /// </summary>
    public MethodInfo Method { get; }

    /// <summary>
    /// Gets the result status.
    /// </summary>
    public TestStatus Status { get; }

    /// <summary>
    /// Gets time taken to run the test.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Gets additional details about the result.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Gets the thrown exception, if any.
    /// </summary>
    public Exception? Exception { get; }
}
