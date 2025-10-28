// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MatrixProduct;

using System.Dynamic;
using static System.Runtime.InteropServices.JavaScript.JSType;

/// <summary>
/// Represents a mathematical matrix of long integers and provides
/// synchronous and multi-threaded asynchronous multiplication operations.
/// </summary>
public class Matrix
{
    private int height;
    private int width;
    private long[][] data;

    /// <summary>
    /// Initializes a new instance of the <see cref="Matrix"/> class with zero height and width.
    /// </summary>
    public Matrix()
    {
        this.data = new long[0][];
        this.height = 0;
        this.width = 0;
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
        this.height = data.Length;
        this.width = data[0].Length;
        for (int i = 0; i < this.height; i++)
        {
            if (this.data[i].Length != this.width)
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
        this.height = matrixText.Length;
        this.data = new long[this.height][];
        this.width = matrixText[0].Split(" ").Length;

        for (int i = 0; i < this.height; i++)
        {
            this.data[i] = matrixText[i].Split(" ").Select(x => Convert.ToInt64(x)).ToArray();
            if (this.data[i].Length != this.width)
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
        if (second == null || second.height != this.height || second.width != this.width)
        {
            return false;
        }

        for (int i = 0; i < this.height; i++)
        {
            if (second.data[i] == null || this.data[i] == null)
            {
                return false;
            }

            for (int j = 0; j < this.width; j++)
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
        if (this.width != second.height)
        {
            throw new ArgumentException("The number of rows in the argument matrix must equal the number of columns in the calling matrix.");
        }

        var secondDataTransposed = new long[second.width][];
        for (int j = 0; j < second.width; j++)
        {
            secondDataTransposed[j] = new long[this.width];
        }

        for (int k = 0; k < this.width; k++)
        {
            var row = second.data[k];
            for (int j = 0; j < second.width; j++)
            {
                secondDataTransposed[j][k] = row[j];
            }
        }

        var newData = new long[this.height][];
        for (int i = 0; i < this.height; i++)
        {
            newData[i] = new long[second.width];
            for (int j = 0; j < second.width; j++)
            {
                for (int k = 0; k < this.width; k++)
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
    public Matrix AsyncProduct(Matrix second, int threadsNumber)
    {
        if (this.width != second.height)
        {
            throw new ArgumentException("The number of rows in the argument matrix must equal the number of columns in the calling matrix.");
        }

        var secondDataTransposed = new long[second.width][];
        for (int j = 0; j < second.width; j++)
        {
            secondDataTransposed[j] = new long[this.width];
        }

        for (int k = 0; k < this.width; k++)
        {
            var row = second.data[k];
            for (int j = 0; j < second.width; j++)
            {
                secondDataTransposed[j][k] = row[j];
            }
        }

        var threads = new Thread[threadsNumber];

        var newData = new long[this.height][];
        for (int i = 0; i < this.height; i++)
        {
            newData[i] = new long[second.width];
        }

        for (int threadIndex = 0; threadIndex < threadsNumber; threadIndex++)
        {
            int index = threadIndex;
            threads[index] = new Thread(() =>
            {
                // Each thread processes a set of rows from the first matrix and multiplies them by every column of the second matrix
                for (int i = index; i < this.height; i += threadsNumber)
                {
                    for (int j = 0; j < second.width; j++)
                    {
                        for (int k = 0; k < this.width; k++)
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
