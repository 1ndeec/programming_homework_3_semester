// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Test3;

using System.Net;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// Entry point and chat application runner.
/// </summary>
public sealed class Program
{
    private readonly TextReader consoleIn;
    private readonly TextWriter consoleOut;
    private readonly TextWriter consoleError;

    /// <summary>
    /// Initializes a new instance of the <see cref="Program"/> class with injected console streams.
    /// </summary>
    /// <param name="consoleIn">Text input source.</param>
    /// <param name="consoleOut">Text output destination.</param>
    /// <param name="consoleError">Text error destination.</param>
    public Program(TextReader consoleIn, TextWriter consoleOut, TextWriter consoleError)
    {
        this.consoleIn = consoleIn ?? throw new ArgumentNullException(nameof(consoleIn));
        this.consoleOut = consoleOut ?? throw new ArgumentNullException(nameof(consoleOut));
        this.consoleError = consoleError ?? throw new ArgumentNullException(nameof(consoleError));
    }

    /// <summary>
    /// Application entry point used by the runtime.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Process exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        var app = new Program(Console.In, Console.Out, Console.Error);
        return await app.RunAsync(args).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs the application logic using the provided arguments.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Exit code.</returns>
    public async Task<int> RunAsync(string[] args)
    {
        try
        {
            if (args.Length == 1 && int.TryParse(args[0], out int portOnly))
            {
                await this.RunServerAsync(portOnly).ConfigureAwait(false);
                return 0;
            }

            if (args.Length == 2 && int.TryParse(args[1], out int port))
            {
                string host = args[0];
                await this.RunClientAsync(host, port).ConfigureAwait(false);
                return 0;
            }

            this.PrintUsage();
            return 2;
        }
        catch (Exception ex)
        {
            await this.consoleOut.WriteLineAsync("Probable wrong input format.").ConfigureAwait(false);
            await this.consoleError.WriteLineAsync($"Fatal error: {ex.Message}").ConfigureAwait(false);
            return 1;
        }
    }

    private void PrintUsage()
    {
        this.consoleOut.WriteLine("Usage:");
        this.consoleOut.WriteLine("  Server: ChatApp <port>");
        this.consoleOut.WriteLine("  Client: ChatApp <ip-or-host> <port>");
        this.consoleOut.WriteLine("Type 'exit' to close the connection from either side.");
    }

    private async Task RunServerAsync(int port)
    {
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();

        this.consoleOut.WriteLine($"Server listening on port {port}...");
        this.consoleOut.WriteLine("Waiting for a client...");

        using TcpClient client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
        listener.Stop();

        this.consoleOut.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");
        await this.RunChatAsync(client, nickname: "Server").ConfigureAwait(false);
    }

    private async Task RunClientAsync(string host, int port)
    {
        using var client = new TcpClient();

        this.consoleOut.WriteLine($"Connecting to {host}:{port} ...");
        await client.ConnectAsync(host, port).ConfigureAwait(false);
        this.consoleOut.WriteLine("Connected.");

        await this.RunChatAsync(client, nickname: "Client").ConfigureAwait(false);
    }

    private async Task RunChatAsync(TcpClient client, string nickname)
    {
        using NetworkStream stream = client.GetStream();

        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        using var reader = new StreamReader(stream, utf8, bufferSize: 4096, leaveOpen: true);
        using var writer = new StreamWriter(stream, utf8, bufferSize: 4096, leaveOpen: true) { AutoFlush = true };

        using var cts = new CancellationTokenSource();

        Task netTask = this.ReadFromNetworkAsync(reader, cts);
        Task consoleTask = this.ReadFromConsoleAsync(writer, cts, nickname);

        await Task.WhenAny(netTask, consoleTask).ConfigureAwait(false);
        cts.Cancel();

        try
        {
            client.Close();
        }
        catch
        {
        }

        try
        {
            await Task.WhenAll(netTask, consoleTask).ConfigureAwait(false);
        }
        catch
        {
        }

        await this.consoleOut.WriteLineAsync("Chat ended.").ConfigureAwait(false);
    }

    private async Task ReadFromNetworkAsync(StreamReader reader, CancellationTokenSource cts)
    {
        try
        {
            while (!cts.IsCancellationRequested)
            {
                string? line = await reader.ReadLineAsync().ConfigureAwait(false);

                if (line is null)
                {
                    await this.consoleOut.WriteLineAsync("Client disconnected.").ConfigureAwait(false);
                    cts.Cancel();
                    return;
                }

                if (this.IsExit(line))
                {
                    await this.consoleOut.WriteLineAsync("Client typed 'exit'. Closing...").ConfigureAwait(false);
                    cts.Cancel();
                    return;
                }

                await this.consoleOut.WriteLineAsync($"Client: {line}").ConfigureAwait(false);
            }
        }
        catch (IOException)
        {
            if (!cts.IsCancellationRequested)
            {
                await this.consoleOut.WriteLineAsync("Network error, shutting down the network.").ConfigureAwait(false);
            }

            cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            cts.Cancel();
        }
    }

    private Task ReadFromConsoleAsync(StreamWriter writer, CancellationTokenSource cts, string roleLabel)
    {
        return Task.Run(async () =>
        {
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    string? line = this.consoleIn.ReadLine();
                    if (line is null)
                    {
                        cts.Cancel();
                        return;
                    }

                    if (this.IsExit(line))
                    {
                        await writer.WriteLineAsync("exit").ConfigureAwait(false);
                        await this.consoleOut.WriteLineAsync($"{roleLabel}: closing...").ConfigureAwait(false);
                        cts.Cancel();
                        return;
                    }

                    await writer.WriteLineAsync(line).ConfigureAwait(false);
                }
            }
            catch (IOException)
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                cts.Cancel();
            }
        });
    }

    private bool IsExit(string s) =>
        string.Equals(s.Trim(), "exit", StringComparison.OrdinalIgnoreCase);
}
