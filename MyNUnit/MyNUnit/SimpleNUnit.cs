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
    public async Task StartTests(string path)
    {
        var dlls = Directory.GetFiles(path, "*.dll");

        var allResults = new List<TestResult>();

        foreach (string dll in dlls)
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

            foreach (var list in classResults)
            {
                allResults.AddRange(list);
            }
        }

        PrintReport(allResults);
    }

    private static async Task<List<TestResult>> RunTestForTypeAsync(Type t, AttributeSet attrs)
    {
        var classResults = new List<TestResult>();

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

        var instance = Activator.CreateInstance(t);

        foreach (var beforeClassMethod in beforeClass)
        {
            if (!beforeClassMethod.IsStatic)
            {
                throw new InvalidOperationException("BeforeClass method must be static");
            }

            await InvokeMaybeAsync(beforeClassMethod, null);
        }

        var testTasks = tests.Select(async testMethod =>
        {
            foreach (var beforeMethod in before)
            {
                await InvokeMaybeAsync(beforeMethod, instance);
            }

            var result = await testMethod.RunAsync(instance);
            classResults.Add(result);

            foreach (var afterMethod in after)
            {
                await InvokeMaybeAsync(afterMethod, instance);
            }
        });

        await Task.WhenAll(testTasks);

        foreach (var afterClassMethod in afterClass)
        {
            if (!afterClassMethod.IsStatic)
            {
                throw new InvalidOperationException("AfterClass method must be static");
            }

            await InvokeMaybeAsync(afterClassMethod, null);
        }

        return classResults;
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
        int passed = 0, failed = 0, skipped = 0;

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
        Console.WriteLine($"Summary: passed={passed}, failed={failed}, skipped={skipped}");
    }
}