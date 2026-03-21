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
    /// Verifies that the runner runs and prints a summary header.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task StartTests_Runs_And_Prints_Summary()
    {
        var path = "../../../testData";
        Assert.IsTrue(Directory.Exists(path), "testData directory not found.");

        var text = await RunAndCaptureAsync(path);
        StringAssert.Contains(text, "Summary:", "Runner must print a summary.");
    }

    /// <summary>
    /// Verifies that the runner reports at least one passed test.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task StartTests_Reports_Passed_Tests()
    {
        var path = "../../../testData";
        var text = await RunAndCaptureAsync(path);

        StringAssert.Contains(text, "PASS", "Runner output should contain at least one PASS.");
    }

    /// <summary>
    /// Verifies that the runner reports at least one failed test when failing tests exist.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task StartTests_Reports_Failed_Tests_If_Any()
    {
        var path = "../../../testData";
        var text = await RunAndCaptureAsync(path);

        StringAssert.Contains(text, "FAIL", "Runner output should contain at least one FAIL.");
    }

    /// <summary>
    /// Verifies that the runner reports at least one skipped test when skipped tests exist.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task StartTests_Reports_Skipped_Tests_If_Any()
    {
        var path = "../../../testData";
        var text = await RunAndCaptureAsync(path);

        StringAssert.Contains(text, "SKIP", "Runner output should contain at least one SKIP.");
    }

    /// <summary>
    /// Verifies that invalid test signatures are reported to the console and do not crash the run.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task StartTests_Prints_Validation_Errors_And_Does_Not_Crash()
    {
        var path = "../../../testData";
        var text = await RunAndCaptureAsync(path);

        StringAssert.Contains(text, "StaticTest_IsInvalid", "Validation output must mention the invalid test method.");
        StringAssert.Contains(text, "must be non-static", "Validation output must explain what is wrong.");
    }

    /// <summary>
    /// Verifies that normal test failures are distinguishable from validation/infra errors.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task StartTests_Distinguishes_Test_Fail_From_Validation_Error()
    {
        var path = "../../../testData";
        var text = await RunAndCaptureAsync(path);

        Assert.IsTrue(
            text.Contains("boom: test body") || text.Contains("boom: test body (should be FAIL"),
            "Should report a normal failing test body.");

        StringAssert.Contains(text, "FAIL", "Should contain FAIL for test-body failure.");

        Assert.IsTrue(
            text.Contains("ERROR") || text.Contains("ERRORED") || text.Contains("must be non-static"),
            "Should contain at least one validation/infra error marker (ERROR/ERRORED or the validation message).");
    }

    /// <summary>
    /// Smoke test for SampleGoodTests.dll: verifies PASS, SKIP, and expected-exception PASS behavior is visible in output.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task GoodDll_Smoke_Prints_Pass_Skip_ExpectedException()
    {
        var path = "../../../testData";
        var text = await RunAndCaptureAsync(path);

        StringAssert.Contains(text, "SampleGoodTests", "Runner should mention SampleGoodTests.dll or its tests.");

        StringAssert.Contains(text, "Add_Passes", "Runner output should mention Add_Passes.");
        StringAssert.Contains(text, "PASS", "Runner output should contain PASS.");

        StringAssert.Contains(text, "DivideByZero_IsExpected", "Runner output should mention DivideByZero_IsExpected.");
        StringAssert.Contains(text, "PASS", "Expected-exception test should be PASS.");

        StringAssert.Contains(text, "Ignored_Test", "Runner output should mention Ignored_Test.");
        StringAssert.Contains(text, "SKIP", "Ignored test should be SKIP.");
    }

    /// <summary>
    /// Smoke test for SampleGoodTests.dll: verifies hook counters were incremented after running tests.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task GoodDll_Smoke_Hooks_Run_Counters_Move()
    {
        var path = "../../../testData";
        _ = await RunAndCaptureAsync(path);

        var asmPath = Path.Combine(path, "SampleGoodTests.dll");
        Assert.IsTrue(File.Exists(asmPath), $"Missing {asmPath}");

        var asm = System.Reflection.Assembly.LoadFrom(asmPath);
        var t = asm.GetType("SampleGoodTests.CalculatorTests", throwOnError: true)!;

        int beforeClass = (int)t.GetProperty("BeforeClassCount")!.GetValue(null)!;
        int afterClass = (int)t.GetProperty("AfterClassCount")!.GetValue(null)!;
        int before = (int)t.GetProperty("BeforeCount")!.GetValue(null)!;
        int after = (int)t.GetProperty("AfterCount")!.GetValue(null)!;

        Assert.IsGreaterThan(0, beforeClass, "BeforeClass should run at least once.");
        Assert.IsGreaterThan(0, afterClass, "AfterClass should run at least once.");
        Assert.IsGreaterThan(0, before, "Before should run at least once.");
        Assert.AreEqual(before, after, "Before/After should be balanced.");
    }

    private static async Task<string> RunAndCaptureAsync(string path)
    {
        var oldOut = Console.Out;
        var output = new StringWriter();

        try
        {
            Console.SetOut(output);
            var runner = new SimpleNUnit();
            await runner.AsyncStartTests(path);
            return output.ToString();
        }
        finally
        {
            Console.SetOut(oldOut);
        }
    }
}
