// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MyOwnServer;

using System.Net.Sockets;
using System.Text;

/// <summary>
/// Simple TCP client that communicates with SimpleServer.
/// </summary>
public class SimpleClient : IDisposable
{
    private TcpClient client;
    private NetworkStream stream;
    private StreamWriter writer;
    private StreamReader reader;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleClient"/> class.
    /// </summary>
    /// <param name="port">Port of the server.</param>
    /// <param name="host">Host ip.</host>
    public SimpleClient(int port, string host = "localhost")
    {
        this.client = new TcpClient(host, port);
        this.stream = this.client.GetStream();
        this.writer = new StreamWriter(this.stream) { AutoFlush = true };
        this.reader = new StreamReader(this.stream);
    }

    /// <summary>
    /// Sends a request to list the contents of the directory at <paramref name="path"/>
    /// and prints each entry received from the server.
    /// </summary>
    /// <param name="path">Relative path of the directory to list.</param>
    /// <param name="ct"> CancellationToken. </param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task<List<string>> AsyncList(string path, CancellationToken ct = default)
    {
        await this.writer.WriteLineAsync($"1 {path}").ConfigureAwait(false);
        var line = await this.reader.ReadLineAsync().ConfigureAwait(false);
        int count = Convert.ToInt32(line);

        var items = new List<string>(count);

        if (count == -1)
        {
            items.Add("Directory not found");
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                line = await this.reader.ReadLineAsync(ct).ConfigureAwait(false);
                items.Add(line ?? string.Empty);
            }
        }

        return items;
    }

    /// <summary>
    /// Requests a file at <paramref name="path"/> and prints its size and content.
    /// </summary>
    /// <param name="path">Relative file path.</param>
    /// <param name="ct"> CancellationToken. </param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task<(int Size, string Content)> AsyncGet(string path, CancellationToken ct = default)
    {
        await this.writer.WriteLineAsync($"2 {path}").ConfigureAwait(false);

        string sizeText = string.Empty;
        while (true)
        {
            var temp = new byte[1];
            int read = await this.stream.ReadAsync(temp, 0, 1).ConfigureAwait(false);
            if (read == 0)
            {
                throw new IOException("Connection closed.");
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
            return (-1, string.Empty);
        }

        byte[] buffer = new byte[size];
        int totalRead = 0;

        while (totalRead < size)
        {
            int read = await this.stream.ReadAsync(buffer, totalRead, size - totalRead).ConfigureAwait(false);
            if (read == 0)
            {
                throw new IOException("Connection closed early.");
            }

            totalRead += read;
        }

        string content = Encoding.UTF8.GetString(buffer);
        return (size, content);
    }

    /// <summary>
    /// Closes the connection and disposes all resources.
    /// </summary>
    public void Close() => this.Dispose();

    /// <summary>
    /// Dispose method.
    /// </summary>
    public void Dispose()
    {
        this.reader.Dispose();
        this.writer.Dispose();
        this.stream.Dispose();
        this.client.Dispose();
    }
}