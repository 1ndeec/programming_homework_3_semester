// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MyOwnServer;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

/// <summary>
/// TCP server that handles simple directory listing and file transfer requests.
/// </summary>
public class SimpleServer
{
    private int port;

    private ConcurrentDictionary<Task, byte> clientTasks = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleServer"/> class.
    /// </summary>
    /// <param name="port">Port on which the server will listen.</param>
    public SimpleServer(int port) => this.port = port;

    /// <summary>
    /// Starts the server loop asynchronously and begins accepting client connections.
    /// </summary>
    /// <param name="ct"> CancellationToken. </param>
    /// <returns>A task representing the running server.</returns>
    public Task StartAsync(CancellationToken ct = default) => this.Run(ct);

    /// <summary>
    /// Main server loop: accepts clients and processes their requests.
    /// </summary>
    /// <returns>A task representing the asynchronous execution of the loop.</returns>
    private async Task Run(CancellationToken ct)
    {
        var listener = new TcpListener(IPAddress.Any, this.port);
        listener.Start();

        using var stop = ct.Register(() => listener.Stop());

        while (!ct.IsCancellationRequested)
        {
            Socket socket;
            try
            {
                socket = await listener.AcceptSocketAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (SocketException) when (ct.IsCancellationRequested)
            {
                break;
            }

            var bin = Task.Run(async () =>
            {
                using var stream = new NetworkStream(socket);
                using var reader = new StreamReader(stream);
                var command = await reader.ReadLineAsync();

                var request = new string[2];
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
                    using (var writer = new StreamWriter(stream))
                    {
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
                }
                else if (request[0] == "2")
                {
                    // Handle file transfer
                    using (var writer = new StreamWriter(stream))
                    {
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
                }
                else
                {
                    return;
                }

                socket.Close();
            });

            this.clientTasks.TryAdd(bin, 0);
            _ = bin.ContinueWith(
                t => this.clientTasks.TryRemove(t, out _),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        await Task.WhenAll(this.clientTasks.Keys).ConfigureAwait(false);

        listener.Stop();
        listener.Dispose();
    }
}