// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MyNUnit;

using System.Diagnostics;
using System.Reflection;

/// <summary>
/// Executes a single test method with support for async, expected exceptions, and skipping.
/// </summary>
public class TestMethod
{
    private readonly MethodInfo method;
    private string? ignoreReason;
    private Type? expectedException;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestMethod"/> class.
    /// </summary>
    /// <param name="data">The reflected test method.</param>
    public TestMethod(MethodInfo data)
    {
        this.method = data;

        foreach (Attribute attr in Attribute.GetCustomAttributes(data))
        {
            var t = attr.GetType();
            if (t.Name != "TestAttribute")
            {
                continue;
            }

            var expectedVal = t.GetProperty("Expected")?.GetValue(attr);
            var ignoreVal = t.GetProperty("Ignore")?.GetValue(attr);

            this.ignoreReason = (string?)ignoreVal;
            this.expectedException = (Type?)expectedVal;
        }
    }

    /// <summary>
    /// Runs the test method and returns its result.
    /// Handles sync and async tests, expected exceptions, and skipped tests.
    /// </summary>
    /// <param name="instance">The object instance to invoke the test on.</param>
    /// <returns>The recorded test result.</returns>
    /// <exception cref="ArgumentException">Thrown when reflection produces invalid state.</exception>
    public async Task<TestResult> RunAsync(object? instance)
    {
        if (this.ignoreReason != null)
        {
            return new TestResult(
                this.method,
                TestStatus.Skipped,
                TimeSpan.Zero,
                this.ignoreReason);
        }

        object? result;
        var sw = Stopwatch.StartNew();

        try
        {
            result = this.method.Invoke(instance, null);

            if (result is not Task)
            {
                sw.Stop();
            }
        }
        catch (TargetInvocationException tie)
        {
            sw.Stop();
            var ex = tie.InnerException;

            if (ex == null)
            {
                throw new ArgumentException("Expected an inner exception.");
            }

            if (this.expectedException == null)
            {
                return new TestResult(
                    this.method,
                    TestStatus.Failed,
                    sw.Elapsed,
                    $"Unexpected exception {ex.GetType()}: {ex.Message}");
            }

            if (ex.GetType() != this.expectedException)
            {
                return new TestResult(
                    this.method,
                    TestStatus.Failed,
                    sw.Elapsed,
                    $"Expected exception was {this.expectedException}, actual one was {ex.GetType()}.");
            }

            return new TestResult(this.method, TestStatus.Passed, sw.Elapsed);
        }

        if (result is Task task)
        {
            try
            {
                await task;
                sw.Stop();
            }
            catch (Exception ex)
            {
                sw.Stop();

                if (this.expectedException == null)
                {
                    return new TestResult(
                        this.method,
                        TestStatus.Failed,
                        sw.Elapsed,
                        $"Unexpected exception {ex.GetType()}: {ex.Message}");
                }

                if (ex.GetType() != this.expectedException)
                {
                    return new TestResult(
                        this.method,
                        TestStatus.Failed,
                        sw.Elapsed,
                        $"Expected exception was {this.expectedException}, actual one was {ex.GetType()}.");
                }

                return new TestResult(this.method, TestStatus.Passed, sw.Elapsed);
            }
        }

        if (this.expectedException != null)
        {
            return new TestResult(
                this.method,
                TestStatus.Failed,
                sw.Elapsed,
                $"Expected exception {this.expectedException}, but none was thrown.");
        }

        return new TestResult(this.method, TestStatus.Passed, sw.Elapsed);
    }
}
