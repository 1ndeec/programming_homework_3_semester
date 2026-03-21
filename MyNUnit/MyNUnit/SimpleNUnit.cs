// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MyNUnit;

using System.Reflection;
using static System.Net.Mime.MediaTypeNames;

/// <summary>
/// Discovers and runs tests from external assemblies.
/// </summary>
public class SimpleNUnit
{
    /// <summary>
    /// Runs all tests found in DLLs in the specified directory.
    /// </summary>
    /// <param name="path">Path to the directory containing test assemblies.</param>
    /// <returns>A task representing the asynchronous test run.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if required test attributes or their properties are not found.
    /// </exception>
    public async Task AsyncStartTests(string path)
    {
        var dlls = Directory.GetFiles(path, "*.dll");

        var dllTasks = dlls.Select(async dll =>
        {
            var a = Assembly.LoadFrom(dll);

            var search = (string name) => a.GetTypes()
                .FirstOrDefault(t => typeof(Attribute).IsAssignableFrom(t) && t.Name == name);

            var extractedTestAttribute = search("TestAttribute")
                ?? throw new ArgumentNullException("TestAttribute not found.");

            ArgumentNullException.ThrowIfNull(extractedTestAttribute.GetProperty("Expected"));
            ArgumentNullException.ThrowIfNull(extractedTestAttribute.GetProperty("Ignore"));

            var extractedBeforeAttribute = search("BeforeAttribute")
                ?? throw new ArgumentNullException("BeforeAttribute not found.");

            var extractedAfterAttribute = search("AfterAttribute")
                ?? throw new ArgumentNullException("AfterAttribute not found.");

            var extractedBeforeClassAttribute = search("BeforeClassAttribute")
                ?? throw new ArgumentNullException("BeforeClassAttribute not found.");

            var extractedAfterClassAttribute = search("AfterClassAttribute")
                ?? throw new ArgumentNullException("AfterClassAttribute not found.");

            var attrs = new AttributeSet(
                extractedTestAttribute,
                extractedBeforeAttribute,
                extractedAfterAttribute,
                extractedBeforeClassAttribute,
                extractedAfterClassAttribute);

            var tasks = a.ExportedTypes.Select(t => RunTestForTypeAsync(t, attrs)).ToList();
            var classResults = await Task.WhenAll(tasks);
            return classResults.SelectMany(x => x);
        });

        var allResults = (await Task.WhenAll(dllTasks)).SelectMany(x => x).ToList();
        PrintReport(allResults);
    }

    private static async Task<List<TestResult>> RunTestForTypeAsync(Type t, AttributeSet attrs)
    {
        var beforeClass = new List<MethodInfo>();
        var afterClass = new List<MethodInfo>();
        var before = new List<MethodInfo>();
        var after = new List<MethodInfo>();
        var tests = new List<TestMethod>();

        var handlers = new Dictionary<Type, Action<MethodInfo>>();
        handlers[attrs.Before] = m => before.Add(m);
        handlers[attrs.After] = m => after.Add(m);
        handlers[attrs.BeforeClass] = m => beforeClass.Add(m);
        handlers[attrs.AfterClass] = m => afterClass.Add(m);
        handlers[attrs.Test] = m => tests.Add(new TestMethod(m));

        foreach (MethodInfo method in t.GetMethods())
        {
            foreach (Attribute attr in Attribute.GetCustomAttributes(method))
            {
                if (handlers.TryGetValue(attr.GetType(), out var handle))
                {
                    handle(method);
                }
            }
        }

        foreach (var m in beforeClass)
        {
            var err = ValidateMethodSignature(m, "BeforeClass", mustBeStatic: true, mustBeInstance: false);
            if (err != null)
            {
                Console.WriteLine(err);
                return tests
                    .Select(tm => new TestResult(tm.Method, TestStatus.Errored, TimeSpan.Zero, err))
                    .ToList();
            }
        }

        foreach (var m in afterClass)
        {
            var err = ValidateMethodSignature(m, "AfterClass", mustBeStatic: true, mustBeInstance: false);
            if (err != null)
            {
                Console.WriteLine(err);
                return tests
                    .Select(tm => new TestResult(tm.Method, TestStatus.Errored, TimeSpan.Zero, err))
                    .ToList();
            }
        }

        foreach (var m in before)
        {
            var err = ValidateMethodSignature(m, "Before", mustBeStatic: false, mustBeInstance: true);
            if (err != null)
            {
                Console.WriteLine(err);
                return tests
                    .Select(tm => new TestResult(tm.Method, TestStatus.Errored, TimeSpan.Zero, err))
                    .ToList();
            }
        }

        foreach (var m in after)
        {
            var err = ValidateMethodSignature(m, "After", mustBeStatic: false, mustBeInstance: true);
            if (err != null)
            {
                Console.WriteLine(err);
                return tests
                    .Select(tm => new TestResult(tm.Method, TestStatus.Errored, TimeSpan.Zero, err))
                    .ToList();
            }
        }

        try
        {
            foreach (var beforeClassMethod in beforeClass)
            {
                await InvokeMaybeAsync(beforeClassMethod, null);
            }
        }
        catch (Exception ex)
        {
            var msg = $"BeforeClass failed in {t.FullName}: {ex.GetType().Name}: {ex.Message}";
            Console.WriteLine(msg);

            return tests.Select(tm => new TestResult(tm.Method, TestStatus.Errored, TimeSpan.Zero, msg)).ToList();
        }

        var testTasks = tests.Select(async test =>
        {
            object instance;
            try
            {
                instance = Activator.CreateInstance(t)!;
            }
            catch (Exception ex)
            {
                var msg = $"Constructor failed for {t.FullName}: {ex.GetType().Name}: {ex.Message}";
                Console.WriteLine(msg);
                return new TestResult(test.Method, TestStatus.Errored, TimeSpan.Zero, msg);
            }

            try
            {
                foreach (var m in before)
                {
                    await InvokeMaybeAsync(m, instance);
                }
            }
            catch (Exception ex)
            {
                var msg = $"Before failed for {t.FullName}.{test.Method.Name}: {ex.GetType().Name}: {ex.Message}";
                Console.WriteLine(msg);
                return new TestResult(test.Method, TestStatus.Errored, TimeSpan.Zero, msg);
            }

            var r = await test.RunAsync(instance);

            try
            {
                foreach (var m in after)
                {
                    await InvokeMaybeAsync(m, instance);
                }
            }
            catch (Exception ex)
            {
                var msg = $"After failed for {t.FullName}.{test.Method.Name}: {ex.GetType().Name}: {ex.Message}";
                Console.WriteLine(msg);
                return new TestResult(test.Method, TestStatus.Errored, r.Duration, msg);
            }

            return r;
        });

        var results = await Task.WhenAll(testTasks);

        try
        {
            foreach (var m in afterClass)
            {
                await InvokeMaybeAsync(m, null);
            }
        }
        catch (Exception ex)
        {
            var msg = $"AfterClass failed in {t.FullName}: {ex.GetType().Name}: {ex.Message}";
            Console.WriteLine(msg);
        }

        return results.ToList();
    }

    private static async Task InvokeMaybeAsync(MethodInfo method, object? instance)
    {
        object? result;
        try
        {
            result = method.Invoke(instance, null);
        }
        catch (TargetInvocationException tie)
        {
            throw tie.InnerException ?? tie;
        }
    }

    private static void PrintReport(IEnumerable<TestResult> results)
    {
        int passed = 0, failed = 0, skipped = 0, errored = 0;

        foreach (var r in results)
        {
            var name = $"{r.Method.DeclaringType?.FullName}.{r.Method.Name}";
            var timeMs = r.Duration.TotalMilliseconds.ToString("F2");

            switch (r.Status)
            {
                case TestStatus.Passed:
                    passed++;
                    Console.WriteLine($"PASS  {name}  ({timeMs} ms)");
                    break;

                case TestStatus.Errored:
                    errored++;
                    Console.WriteLine($"ERROR  {name}  ({timeMs} ms)");
                    break;

                case TestStatus.Failed:
                    failed++;
                    Console.WriteLine($"FAIL  {name}  ({timeMs} ms)  {r.Message}");
                    break;

                case TestStatus.Skipped:
                    skipped++;
                    Console.WriteLine($"SKIP  {name}  (reason: {r.Message})");
                    break;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Summary: passed={passed}, failed={failed}, skipped={skipped}, errored={errored}");
    }

    private static string? ValidateMethodSignature(MethodInfo m, string role, bool mustBeStatic, bool mustBeInstance)
    {
        if (mustBeStatic && !m.IsStatic)
        {
            return $"{role} method '{m.DeclaringType?.FullName}.{m.Name}' must be static.";
        }

        if (mustBeInstance && m.IsStatic)
        {
            return $"{role} method '{m.DeclaringType?.FullName}.{m.Name}' must be non-static.";
        }

        if (m.GetParameters().Length != 0)
        {
            return $"{role} method '{m.DeclaringType?.FullName}.{m.Name}' must not take parameters.";
        }

        var rt = m.ReturnType;
        if (rt != typeof(void) && rt != typeof(Task))
        {
            return $"{role} method '{m.DeclaringType?.FullName}.{m.Name}' must return void or Task (actual: {rt}).";
        }

        return null;
    }
}