// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace WebDebug.Domain;

/// <summary>
/// Represents the persisted result of a single executed test case.
/// Stores identifying info, execution outcome, timing, and an optional message.
/// </summary>
public class TestCaseResult
{
    /// <summary>
    /// Gets or sets primary key of the test case result record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets foreign key referencing the parent test run this result belongs to.
    /// </summary>
    public Guid RunId { get; set; }

    /// <summary>
    /// Gets or sets fully qualified test method name (typically "Namespace.Type.Method").
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets execution status of the test (e.g. Passed, Failed, Skipped).
    /// Stored as text for display and simplicity.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets test execution duration in milliseconds.
    /// </summary>
    public double DurationMs { get; set; }

    /// <summary>
    /// Gets or sets optional message associated with the result (failure reason, skip reason, etc.).
    /// </summary>
    public string? Message { get; set; }
}
