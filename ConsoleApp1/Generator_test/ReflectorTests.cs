// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using Generator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Test class.
/// </summary>
[TestClass]
[DoNotParallelize]
public class ReflectorTests
{
    /// <summary>
    /// Verifies that PrintStructure creates a source file for the given type.
    /// </summary>
    [TestMethod]
    public void PrintStructure_CreatesFile()
    {
        Reflector.PrintStructure(typeof(TestClassA));

        var fileName = "TestClassA.cs";
        Assert.IsTrue(File.Exists(fileName));
    }

    /// <summary>
    /// Verifies that the generated file contains the class declaration.
    /// </summary>
    [TestMethod]
    public void PrintStructure_FileContainsClassName()
    {
        Reflector.PrintStructure(typeof(TestClassA));

        var text = File.ReadAllText("TestClassA.cs");
        StringAssert.Contains(text, "class TestClassA");
    }

    /// <summary>
    /// Verifies that DiffClasses writes some output to the provided stream.
    /// </summary>
    [TestMethod]
    public void DiffClasses_WritesOutput()
    {
        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);

        Reflector.DiffClasses(typeof(TestClassA), typeof(TestClassB), writer);

        Assert.IsGreaterThan(0, sb.Length);
    }

    /// <summary>
    /// Verifies that DiffClasses reports field differences.
    /// </summary>
    [TestMethod]
    public void DiffClasses_ReportsDifferentFields()
    {
        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);

        Reflector.DiffClasses(typeof(TestClassA), typeof(TestClassB), writer);

        var output = sb.ToString();
        StringAssert.Contains(output, "FIELDS");
    }

    /// <summary>
    /// Verifies that DiffClasses reports method differences.
    /// </summary>
    [TestMethod]
    public void DiffClasses_ReportsDifferentMethods()
    {
        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);

        Reflector.DiffClasses(typeof(TestClassA), typeof(TestClassB), writer);

        var output = sb.ToString();
        StringAssert.Contains(output, "METHODS");
    }
}