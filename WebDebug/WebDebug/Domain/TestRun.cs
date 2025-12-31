// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace WebDebug.Domain;

/// <summary>
/// Represents a single stored execution of the test runner.
/// Contains summary counts and the collection of individual test case results.
/// </summary>
public class TestRun
{
    /// <summary>
    /// Gets or sets primary key of the test run.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets uTC timestamp when the run was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets total number of discovered/executed tests in this run.
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Gets or sets number of tests that completed with status Passed.
    /// </summary>
    public int Passed { get; set; }

    /// <summary>
    /// Gets or sets number of tests that completed with status Failed.
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    /// Gets or sets number of tests that were skipped.
    /// </summary>
    public int Skipped { get; set; }

    /// <summary>
    /// Gets or sets navigation property for the test case results belonging to this run.
    /// </summary>
    public List<TestCaseResult> Results { get; set; } = new();
}
