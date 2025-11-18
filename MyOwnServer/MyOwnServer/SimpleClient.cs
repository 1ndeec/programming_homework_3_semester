// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MyOwnServer;

using System.Net.Sockets;
using System.Text;

/// <summary>
/// Simple TCP client that communicates with SimpleServer.
/// </summary>
public class SimpleClient
{
    private TcpClient client;
    private NetworkStream stream;
    private StreamWriter writer;
    private StreamReader reader;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleClient"/> class.
    /// </summary>
    /// <param name="port">Port of the server.</param>
    public SimpleClient(int port)
    {
        this.client = new TcpClient("localhost", port);
        this.stream = this.client.GetStream();
        this.writer = new StreamWriter(this.stream) { AutoFlush = true };
        this.reader = new StreamReader(this.stream);
    }

    /// <summary>
    /// Sends a request to list the contents of the directory at <paramref name="path"/> 
    /// and prints each entry received from the server.
    /// </summary>
    /// <param name="path">Relative path of the directory to list.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task List(string path)
    {
        this.writer.WriteLine($"1 {path}");
        int count = Convert.ToInt32(await this.reader.ReadLineAsync());
        if (count == -1)
        {
            Console.WriteLine("Directory not found");
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                Console.WriteLine(await this.reader.ReadLineAsync());
            }
        }
    }

    /// <summary>
    /// Requests a file at <paramref name="path"/> and prints its size and content.
    /// </summary>
    /// <param name="path">Relative file path.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Get(string path)
    {
        this.writer.WriteLine($"2 {path}");

        string sizeText = string.Empty;
        while (true)
        {
            var temp = new byte[1];
            int read = await this.stream.ReadAsync(temp, 0, 1);
            if (read == 0)
            {
                Console.WriteLine("Connection closed");
                return;
            }

            if (temp[0] == (byte)' ' || temp[0] == (byte)'\n')
            {
                break;
            }

            sizeText += (char)temp[0];
        }

        int size = int.Parse(sizeText);
        if (size == -1)
        {
            Console.WriteLine("File not found");
            return;
        }

        Console.Write($"{size} ");

        byte[] buffer = new byte[size];
        int totalRead = 0;
        while (totalRead < size)
        {
            int read = await this.stream.ReadAsync(buffer, totalRead, size - totalRead);
            if (read == 0)
            {
                Console.WriteLine("Connection closed early");
                return;
            }

            Console.Write(Encoding.UTF8.GetString(buffer));
            totalRead += read;
        }

        Console.Write("\n");
    }

    /// <summary>
    /// Closes the connection and disposes all resources.
    /// </summary>
    public void Close()
    {
        this.writer.Dispose();
        this.stream.Dispose();
        this.client.Close();
    }
}