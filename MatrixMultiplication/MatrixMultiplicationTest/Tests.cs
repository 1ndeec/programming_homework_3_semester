// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MatrixProduct_Test;

using System.Diagnostics;
using MatrixMultiplication;

/// <summary>
/// Unit tests for verifying synchronous and multi-threaded matrix multiplication,
/// including timing runs and simple statistics reported to a file.
/// </summary>
[DoNotParallelizeAttribute]
[TestClass]
public sealed class Tests
{
    /// <summary>
    /// Verifies correctness and writes performance statistics for a small sample.
    /// </summary>
    [TestMethod]
    public void SmallSampleTest()
    {
        string firstPath = "../../../TestData/Sample1_first.txt";
        string secondPath = "../../../TestData/Sample1_second.txt";
        string outputPath = "../../../TestData/Sample1_report.txt";

        Assert.IsTrue(this.BringStatistics(firstPath, secondPath, outputPath));
    }

    /// <summary>
    /// Verifies correctness and writes performance statistics for two 1000×1000 matrices.
    /// </summary>
    [TestMethod]
    public void BigSampleTest1()
    {
        // matrices 1000x1000, values from 0 to 10
        string firstPath = "../../../TestData/Sample2_first.txt";
        string secondPath = "../../../TestData/Sample2_second.txt";
        string outputPath = "../../../TestData/Sample2_report.txt";

        Assert.IsTrue(this.BringStatistics(firstPath, secondPath, outputPath));
    }

    /// <summary>
    /// Verifies correctness and writes performance statistics for 500×1000 multiplied by 1000×500.
    /// </summary>
    [TestMethod]
    public void BigSampleTest2()
    {
        // first 500x1000, second 1000x500, values from 0 to 10
        string firstPath = "../../../TestData/Sample3_first.txt";
        string secondPath = "../../../TestData/Sample3_second.txt";
        string outputPath = "../../../TestData/Sample3_report.txt";

        Assert.IsTrue(this.BringStatistics(firstPath, secondPath, outputPath));
    }

    /// <summary>
    /// Verifies correctness and writes performance statistics for 1000×500 multiplied by 500×1000.
    /// </summary>
    [TestMethod]
    public void BigSampleTest3()
    {
        // first 1000x500, second 500x1000, values from 0 to 10
        string firstPath = "../../../TestData/Sample4_first.txt";
        string secondPath = "../../../TestData/Sample4_second.txt";
        string outputPath = "../../../TestData/Sample4_report.txt";

        Assert.IsTrue(MatStat.BringStatistics(firstPath, secondPath, outputPath));
    }
}
