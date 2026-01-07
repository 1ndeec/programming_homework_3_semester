// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MatrixMultiplication;

using System.Diagnostics;

/// <summary>
/// Class for statistical methods used for analytics.
/// </summary>
public class MatStat
{
    /// <summary>
    /// Runs repeated synchronous and asynchronous multiplications, measures elapsed time,
    /// computes expectation (mean) and standard deviation, writes the results to <paramref name="pathOutput"/>,
    /// and verifies that async and sync results are identical.
    /// </summary>
    /// <param name="firstPath">Path to the left matrix file.</param>
    /// <param name="secondPath">Path to the right matrix file.</param>
    /// <param name="pathOutput">Path to the report file to append statistics.</param>
    /// <returns><c>true</c> if the asynchronous and synchronous results are equal; otherwise, <c>false</c>.</returns>
    public static bool BringStatistics(string firstPath, string secondPath, string pathOutput)
    {
        int repetitionNumber = 20;
        int threadNumber = 16;

        Matrix syncAnswer = new();
        Matrix asyncAnswer = new();

        var leftMatrix = new Matrix(firstPath);
        var rightMatrix = new Matrix(secondPath);

        long[] timeRecord = new long[repetitionNumber];

        // --- Sync runs ---
        for (int i = 0; i < repetitionNumber; i++)
        {
            var syncTime = Stopwatch.StartNew();

            syncAnswer = leftMatrix.SyncProduct(rightMatrix);

            syncTime.Stop();
            timeRecord[i] = syncTime.ElapsedTicks;
        }

        using (var sw = new StreamWriter(pathOutput))
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

                asyncAnswer = leftMatrix.ParalellProduct(rightMatrix, i + 1);

                asyncTime.Stop();

                timeRecord[j] = asyncTime.ElapsedTicks;
            }

            if (!syncAnswer.Equals(asyncAnswer))
            {
                return false;
            }

            using (var sw = new StreamWriter(pathOutput, append: true))
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