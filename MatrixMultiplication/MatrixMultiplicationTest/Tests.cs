// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MatrixProduct_Test;

using System.Diagnostics;
using MatrixProduct;

/// <summary>
/// Unit tests for verifying synchronous and multi-threaded matrix multiplication,
/// including timing runs and simple statistics reported to a file.
/// </summary>
[DoNotParallelizeAttribute]
[TestClass]
public sealed class Tests
{
    ///// <summary>
    ///// Verifies correctness and writes performance statistics for a small sample.
    ///// </summary>
    //[TestMethod]
    //public void SmallSampleTest()
    //{
    //    string firstPath = "../../../TestData/Sample1_first.txt";
    //    string secondPath = "../../../TestData/Sample1_second.txt";
    //    string outputPath = "../../../TestData/Sample1_report.txt";

    //    Assert.IsTrue(this.BringStatistics(firstPath, secondPath, outputPath));
    //}

    ///// <summary>
    ///// Verifies correctness and writes performance statistics for two 1000×1000 matrices.
    ///// </summary>
    //[TestMethod]
    //public void BigSampleTest1()
    //{
    //    // matrices 1000x1000, values from 0 to 10
    //    string firstPath = "../../../TestData/Sample2_first.txt";
    //    string secondPath = "../../../TestData/Sample2_second.txt";
    //    string outputPath = "../../../TestData/Sample2_report.txt";

    //    Assert.IsTrue(this.BringStatistics(firstPath, secondPath, outputPath));
    //}

    ///// <summary>
    ///// Verifies correctness and writes performance statistics for 500×1000 multiplied by 1000×500.
    ///// </summary>
    //[TestMethod]
    //public void BigSampleTest2()
    //{
    //    // first 500x1000, second 1000x500, values from 0 to 10
    //    string firstPath = "../../../TestData/Sample3_first.txt";
    //    string secondPath = "../../../TestData/Sample3_second.txt";
    //    string outputPath = "../../../TestData/Sample3_report.txt";

    //    Assert.IsTrue(this.BringStatistics(firstPath, secondPath, outputPath));
    //}

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

        Assert.IsTrue(this.BringStatistics(firstPath, secondPath, outputPath));
    }

    /// <summary>
    /// Runs repeated synchronous and asynchronous multiplications, measures elapsed time,
    /// computes expectation (mean) and standard deviation, writes the results to <paramref name="pathOutput"/>,
    /// and verifies that async and sync results are identical.
    /// </summary>
    /// <param name="firstPath">Path to the left matrix file.</param>
    /// <param name="secondPath">Path to the right matrix file.</param>
    /// <param name="pathOutput">Path to the report file to append statistics.</param>
    /// <returns><c>true</c> if the asynchronous and synchronous results are equal; otherwise, <c>false</c>.</returns>
    private bool BringStatistics(string firstPath, string secondPath, string pathOutput)
    {
        int repetitionNumber = 20;
        int threadNumber = 16;

        Matrix syncAnswer = new();
        Matrix asyncAnswer = new();

        Matrix leftMatrix = new Matrix(firstPath);
        Matrix rightMatrix = new Matrix(secondPath);

        long[] timeRecord = new long[repetitionNumber];

        // --- Sync runs ---
        for (int i = 0; i < repetitionNumber; i++)
        {
            var syncTime = Stopwatch.StartNew();

            syncAnswer = leftMatrix.SyncProduct(rightMatrix);

            syncTime.Stop();
            timeRecord[i] = syncTime.ElapsedTicks;
        }

        using (StreamWriter sw = new StreamWriter(pathOutput))
        {
            long expectation = timeRecord.Sum() / repetitionNumber;
            sw.WriteLine($"Sync expectation: {expectation} ticks");
            long standardDeviation = 0;
            for (int i = 0; i < repetitionNumber; i++)
            {
                standardDeviation += Convert.ToInt64(Math.Pow(timeRecord[i] - expectation, 2));
            }

            standardDeviation = Convert.ToInt64(Math.Sqrt(standardDeviation / repetitionNumber));
            sw.WriteLine($"Sync standard deviation: {standardDeviation} ticks");
        }

        // --- Async runs for 1..threadNumber threads ---
        for (int i = 0; i < threadNumber; i++)
        {
            for (int j = 0; j < repetitionNumber; j++)
            {
                var asyncTime = Stopwatch.StartNew();

                asyncAnswer = leftMatrix.AsyncProduct(rightMatrix, i + 1);

                asyncTime.Stop();

                timeRecord[j] = asyncTime.ElapsedTicks;
            }

            if (!syncAnswer.Equals(asyncAnswer))
            {
                return false;
            }

            using (StreamWriter sw = new StreamWriter(pathOutput, append: true))
            {
                long expectation = timeRecord.Sum() / repetitionNumber;
                sw.WriteLine($"Number of threads: {i + 1}, async expectation: {expectation} ticks");
                long standardDeviation = 0;
                for (int j = 0; j < repetitionNumber; j++)
                {
                    standardDeviation += Convert.ToInt64(Math.Pow(timeRecord[j] - expectation, 2));
                }

                standardDeviation = Convert.ToInt64(Math.Sqrt(standardDeviation / repetitionNumber));
                sw.WriteLine($"Number of threads: {i + 1}, async standard deviation: {standardDeviation} ticks");
            }
        }

        return true;
    }
}
