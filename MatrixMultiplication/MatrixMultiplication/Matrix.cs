// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MatrixMultiplication;

using System.Dynamic;
using static System.Runtime.InteropServices.JavaScript.JSType;

/// <summary>
/// Represents a mathematical matrix of long integers and provides
/// synchronous and multi-threaded asynchronous multiplication operations.
/// </summary>
public class Matrix
{
    private long[][] data;

    /// <summary>
    /// Initializes a new instance of the <see cref="Matrix"/> class with zero height and width.
    /// </summary>
    public Matrix()
    {
        this.data = new long[0][];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Matrix"/> class
    /// from a jagged array of long values.
    /// </summary>
    /// <param name="data">Two-dimensional jagged array representing the matrix values.</param>
    /// <exception cref="InvalidDataException">Thrown when the rows of the array
    /// have inconsistent lengths.</exception>
    public Matrix(long[][] data)
    {
        this.data = data;
        int height = data.Length;
        int width = data[0].Length;
        for (int i = 0; i < height; i++)
        {
            if (this.data[i].Length != width)
            {
                throw new InvalidDataException("Matrix cant contain strings of different lengths");
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Matrix"/> class
    /// by reading matrix data from a text file.
    /// </summary>
    /// <param name="path">The file path containing matrix values separated by spaces.</param>
    /// <exception cref="InvalidDataException">Thrown when the rows in the file
    /// have inconsistent lengths.</exception>
    public Matrix(string path)
    {
        string[] matrixText = File.ReadAllLines(path);
        int height = matrixText.Length;
        this.data = new long[height][];
        int width = matrixText[0].Split(" ").Length;

        for (int i = 0; i < height; i++)
        {
            this.data[i] = matrixText[i].Split(" ").Select(x => Convert.ToInt64(x)).ToArray();
            if (this.data[i].Length != width)
            {
                throw new InvalidDataException("Matrix cant contain strings of different lengths");
            }
        }
    }

    /// <summary>
    /// Determines whether the current matrix is equal to another matrix.
    /// </summary>
    /// <param name="second">The matrix to compare with the current matrix.</param>
    /// <returns><c>true</c> if the matrices have the same dimensions and values; otherwise, <c>false</c>.</returns>
    public bool Equals(Matrix second)
    {
        int secondHeight = second.data.Length;
        int thisHeight = this.data.Length;
        int secondWidth = second.data[0].Length;
        int thisWidth = this.data[0].Length;
        if (second == null || secondHeight != thisHeight || secondWidth != thisWidth)
        {
            return false;
        }

        for (int i = 0; i < thisHeight; i++)
        {
            if (second.data[i] == null || this.data[i] == null)
            {
                return false;
            }

            for (int j = 0; j < thisWidth; j++)
            {
                if (this.data[i][j] != second.data[i][j])
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Performs a synchronous matrix multiplication.
    /// </summary>
    /// <param name="second">The right-hand side matrix for multiplication.</param>
    /// <returns>A new <see cref="Matrix"/> containing the result of the multiplication.</returns>
    /// <exception cref="ArgumentException">Thrown when the number of columns
    /// in the first matrix does not equal the number of rows in the second.</exception>
    public Matrix SyncProduct(Matrix second)
    {
        int secondHeight = second.data.Length;
        int thisHeight = this.data.Length;
        int secondWidth = second.data[0].Length;
        int thisWidth = this.data[0].Length;
        if (thisWidth != secondHeight)
        {
            throw new ArgumentException("The number of rows in the argument matrix must equal the number of columns in the calling matrix.");
        }

        var secondDataTransposed = new long[secondWidth][];
        for (int j = 0; j < secondWidth; j++)
        {
            secondDataTransposed[j] = new long[thisWidth];
        }

        for (int k = 0; k < thisWidth; k++)
        {
            var row = second.data[k];
            for (int j = 0; j < secondWidth; j++)
            {
                secondDataTransposed[j][k] = row[j];
            }
        }

        var newData = new long[thisHeight][];
        for (int i = 0; i < thisHeight; i++)
        {
            newData[i] = new long[secondWidth];
            for (int j = 0; j < secondWidth; j++)
            {
                for (int k = 0; k < thisWidth; k++)
                {
                    newData[i][j] += this.data[i][k] * secondDataTransposed[j][k];
                }
            }
        }

        return new Matrix(newData);
    }

    /// <summary>
    /// Performs an asynchronous matrix multiplication using multiple threads.
    /// </summary>
    /// <param name="second">The right-hand side matrix for multiplication.</param>
    /// <param name="threadsNumber">The number of threads to use for computation.</param>
    /// <returns>A new <see cref="Matrix"/> containing the result of the multiplication.</returns>
    /// <exception cref="ArgumentException">Thrown when the number of columns
    /// in the first matrix does not equal the number of rows in the second.</exception>
    public Matrix ParalellProduct(Matrix second, int threadsNumber)
    {
        int secondHeight = second.data.Length;
        int thisHeight = this.data.Length;
        int secondWidth = second.data[0].Length;
        int thisWidth = this.data[0].Length;
        if (thisWidth != secondHeight)
        {
            throw new ArgumentException("The number of rows in the argument matrix must equal the number of columns in the calling matrix.");
        }

        var secondDataTransposed = new long[secondWidth][];
        for (int j = 0; j < secondWidth; j++)
        {
            secondDataTransposed[j] = new long[thisWidth];
        }

        for (int k = 0; k < thisWidth; k++)
        {
            var row = second.data[k];
            for (int j = 0; j < secondWidth; j++)
            {
                secondDataTransposed[j][k] = row[j];
            }
        }

        var threads = new Thread[threadsNumber];

        var newData = new long[thisHeight][];
        for (int i = 0; i < thisHeight; i++)
        {
            newData[i] = new long[secondWidth];
        }

        for (int threadIndex = 0; threadIndex < threadsNumber; threadIndex++)
        {
            int index = threadIndex;
            threads[index] = new Thread(() =>
            {
                // Each thread processes a set of rows from the first matrix and multiplies them by every column of the second matrix
                for (int i = index; i < thisHeight; i += threadsNumber)
                {
                    for (int j = 0; j < secondWidth; j++)
                    {
                        for (int k = 0; k < thisWidth; k++)
                        {
                            newData[i][j] += this.data[i][k] * secondDataTransposed[j][k];
                        }
                    }
                }
            });
        }

        foreach (var th in threads)
        {
            th.Start();
        }

        foreach (var th in threads)
        {
            th.Join();
        }

        return new Matrix(newData);
    }
}