# C# Course Homeworks

This repository contains solutions for four course assignments covering parallel programming, lazy evaluation, custom task scheduling, and reflection-based test execution in C#.

## Parallel Matrix Multiplication

This project implements parallel multiplication of dense integer matrices using only the `Thread` class for concurrency.

The program reads two matrices from files, multiplies them, and writes the resulting matrix to an output file. In addition to the parallel version, the project includes a sequential implementation in order to compare performance.

Benchmarking is performed on matrices of different sizes. For each configuration, multiple runs are executed so that average execution time and standard deviation can be measured. This makes it possible to evaluate how much acceleration is actually achieved and for which input sizes parallelization becomes worthwhile.

## Lazy

This project implements lazy evaluation through the following interface:

```csharp
public interface ILazy<T>
{
    T Get();
}
```

An `ILazy<T>` object stores a supplier function and computes its value only on the first call to `Get()`. Every later call returns the same cached result.

Two implementations are provided:

* a simple version intended for correct use in a single-threaded environment;
* a thread-safe version intended for multithreaded use.

Both implementations guarantee that the supplier is executed no more than once. The thread-safe version is designed so that synchronization overhead is minimized after the value has already been computed. The implementation also correctly handles cases where the supplier returns `null`.

## MyThreadPool

This project implements a simplified fixed-size thread pool similar to the basic idea of `ThreadPool` and `Task`, but built manually without using the standard TPL abstractions.

When a `MyThreadPool` instance is created, it starts a fixed number of worker threads. Submitted tasks are placed into a shared queue and picked up by available workers. Each task is represented by the `IMyTask<TResult>` interface.

Supported functionality includes:

* asynchronous execution of submitted computations;
* checking whether a task has completed;
* retrieving the result of a completed task;
* waiting for the result if it is not ready yet;
* propagating task failures through `AggregateException`;
* creating continuation tasks with `ContinueWith(...)`.

The pool also supports cooperative shutdown. Already running tasks are not interrupted, while the behavior of waiting tasks is handled consistently with the shutdown policy chosen in the implementation. Special attention is paid to thread safety, non-blocking continuation scheduling, and correct synchronization between worker threads and task consumers.

## MyNUnit

This project implements a command-line test runner that discovers and runs tests in assemblies located under a specified path.

A method is treated as a test if it is marked with the `Test` attribute. The framework also supports additional metadata and lifecycle hooks similar to those found in unit testing libraries.

Supported features include:

* test methods marked with `[Test]`;
* expected exceptions for negative test cases;
* ignored tests with a textual reason;
* `[Before]` and `[After]` methods executed around each individual test;
* `[BeforeClass]` and `[AfterClass]` methods executed once per test class;
* parallel test execution where possible.

The runner prints a report to standard output describing:

* which tests passed and failed;
* execution time for completed tests;
* reasons why tests were ignored;
* diagnostic information for failures.

The project also includes unit tests for the framework itself, written using a separate testing library rather than the framework being developed.

## Notes

All projects were implemented under the course restrictions for the corresponding assignments. In particular, the concurrency-related tasks rely on low-level threading and synchronization mechanisms instead of high-level built-in solutions where their use was forbidden.

