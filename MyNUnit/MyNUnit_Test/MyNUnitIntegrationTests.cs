// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MyNUnit_Test;

using MyNUnit;

/// <summary>
/// Integration tests for the MyNUnit runner.
/// </summary>
[TestClass]
public class MyNUnitIntegrationTests
{
    /// <summary>
    /// Checks that the runner prints a summary line.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [TestMethod]
    public async Task StartTests_Runs_And_Prints_Summary()
    {
        var path = "../../../testData";
        Assert.IsTrue(Directory.Exists(path), "testData directory not found.");

        var runner = new SimpleNUnit();
        var output = new StringWriter();
        Console.SetOut(output);

        await runner.StartTests(path);
        var text = output.ToString();

        Assert.Contains("Summary:", text);
    }

    /// <summary>
    /// Checks that at least one test passes.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [TestMethod]
    public async Task StartTests_Reports_Passed_Tests()
    {
        var path = "../../../testData";
        var runner = new SimpleNUnit();
        var output = new StringWriter();
        Console.SetOut(output);

        await runner.StartTests(path);
        var text = output.ToString();

        Assert.Contains("PASS", text);
    }

    /// <summary>
    /// Checks that failing tests are reported.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [TestMethod]
    public async Task StartTests_Reports_Failed_Tests_If_Any()
    {
        var path = "../../../testData";
        var runner = new SimpleNUnit();
        var output = new StringWriter();
        Console.SetOut(output);

        await runner.StartTests(path);
        var text = output.ToString();

        Assert.Contains("FAIL", text);
    }

    /// <summary>
    /// Checks that skipped tests are reported.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [TestMethod]
    public async Task StartTests_Reports_Skipped_Tests_If_Any()
    {
        var path = "../../../testData";
        var runner = new SimpleNUnit();
        var output = new StringWriter();
        Console.SetOut(output);

        await runner.StartTests(path);
        var text = output.ToString();

        Assert.Contains("SKIP", text);
    }
}
