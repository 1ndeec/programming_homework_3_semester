// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MyNUnit;

/// <summary>
/// Holds the attribute types used to identify test lifecycle methods.
/// </summary>
/// <param name="Test">The test attribute type.</param>
/// <param name="Before">Attribute marking methods that run before each test.</param>
/// <param name="After">Attribute marking methods that run after each test.</param>
/// <param name="BeforeClass">Attribute for static setup before all tests in a class.</param>
/// <param name="AfterClass">Attribute for static teardown after all tests in a class.</param>
public record AttributeSet(
    Type Test,
    Type Before,
    Type After,
    Type BeforeClass,
    Type AfterClass);
