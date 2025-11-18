// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MyOwnServer;

using System.Net;
using System.Net.Sockets;

/// <summary>
/// TCP server that handles simple directory listing and file transfer requests.
/// </summary>
public class SimpleServer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleServer"/> class.
    /// </summary>
    /// <param name="port">Port on which the server will listen.</param>
    public SimpleServer(int port)
    {
        this.Port = port;
    }

    private int Port { get; }

    /// <summary>
    /// Starts the server loop asynchronously and begins accepting client connections.
    /// </summary>
    /// <returns>A task representing the running server.</returns>
    public Task StartAsync()
    {
        return this.Run();
    }

    /// <summary>
    /// Main server loop: accepts clients and processes their requests.
    /// </summary>
    /// <returns>A task representing the asynchronous execution of the loop.</returns>
    private async Task Run()
    {
        var listener = new TcpListener(IPAddress.Any, this.Port);
        listener.Start();
        while (true)
        {
            var socket = await listener.AcceptSocketAsync();

            var bin = Task.Run(async () =>
            {
                using var stream = new NetworkStream(socket);
                using var reader = new StreamReader(stream);
                var command = await reader.ReadLineAsync();

                string[] request = new string[2];
                if (command != null)
                {
                    request = command.Split(' ');
                    if (request.Length != 2)
                    {
                        return;
                    }
                }

                var path = "../../../" + request[1];

                if (request[0] == "1")
                {
                    // Handle directory listing
                    var writer = new StreamWriter(stream);
                    if (Directory.Exists(path))
                    {
                        var entries = Directory.GetFileSystemEntries(path);
                        await writer.WriteLineAsync(entries.Length.ToString());

                        foreach (var entry in entries)
                        {
                            var name = Path.GetFileName(entry);
                            var isDir = Directory.Exists(entry) ? "true" : "false";
                            await writer.WriteLineAsync($"{name} {isDir}");
                        }
                    }
                    else
                    {
                        await writer.WriteLineAsync("-1");
                    }

                    await writer.FlushAsync();
                }
                else if (request[0] == "2")
                {
                    // Handle file transfer
                    var writer = new StreamWriter(stream);

                    if (File.Exists(path))
                    {
                        var data = await File.ReadAllBytesAsync(path);

                        await writer.WriteAsync(data.Length.ToString());
                        await writer.WriteAsync(' ');
                        await writer.FlushAsync();

                        await stream.WriteAsync(data);
                        await stream.FlushAsync();
                    }
                    else
                    {
                        await writer.WriteLineAsync($"-1");
                    }

                    await writer.FlushAsync();
                }
                else
                {
                    return;
                }

                socket.Close();
            });
        }
    }
}