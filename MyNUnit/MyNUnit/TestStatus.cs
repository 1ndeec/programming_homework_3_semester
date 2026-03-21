// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    /// <summary>
    /// The test wasnt executed due to the error.
    /// </summary>
    Errored,
}