// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Test3.Test;

using System.Net;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test3;

/// <summary>
/// MSTest integration tests for the Program class using injected console streams and fixed ports.
/// </summary>
[TestClass]
public class ChatTests
{
    /// <summary>
    /// Fixed port used by tests. If this port is already taken on your machine, tests will be marked inconclusive.
    /// </summary>
    private const int TestPort = 40123;

    /// <summary>
    /// Fixed port used to simulate a failed connection attempt. If something is listening there, the test will be inconclusive.
    /// </summary>
    private const int FailPort = 40124;

    /// <summary>
    /// Verifies that invalid argument shapes cause usage to be printed and exit code 2 to be returned.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task RunAsync_InvalidArgs_PrintsUsage_AndReturns2()
    {
        var input = new StringReader(string.Empty);
        var output = new StringWriter();
        var error = new StringWriter();

        var app = new Program(input, output, error);

        int code = await app.RunAsync(Array.Empty<string>());

        Assert.AreEqual(2, code);
        StringAssert.Contains(output.ToString(), "Usage:");
        StringAssert.Contains(output.ToString(), "Server:");
        StringAssert.Contains(output.ToString(), "Client:");
    }

    /// <summary>
    /// Verifies that a connection failure in client mode is handled and returns exit code 1.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    [Timeout(8000, CooperativeCancellation = true)]
    public async Task RunAsync_ClientConnectFails_Returns1_AndPrintsError()
    {
        if (await IsPortOpenAsync(FailPort))
        {
            Assert.Inconclusive($"FailPort {FailPort} is already in use; cannot reliably test connection failure.");
        }

        var input = new StringReader(string.Empty);
        var output = new StringWriter();
        var error = new StringWriter();

        var app = new Program(input, output, error);

        int code = await app.RunAsync(new[] { "127.0.0.1", FailPort.ToString() });

        Assert.AreEqual(1, code);
        StringAssert.Contains(output.ToString(), "Probable wrong input format.");
        StringAssert.Contains(error.ToString(), "Fatal error:");
    }

    /// <summary>
    /// Verifies that server mode returns 0 after the client connects and then closes without sending any lines.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    [Timeout(8000, CooperativeCancellation = true)]
    public async Task RunAsync_Server_ClientDisconnects_Returns0_AndPrintsDisconnect()
    {
        if (await IsPortOpenAsync(TestPort))
        {
            Assert.Inconclusive($"TestPort {TestPort} is already in use; choose another fixed port.");
        }

        var serverIn = new StringReader(string.Empty);
        var serverOut = new StringWriter();
        var serverErr = new StringWriter();

        var serverApp = new Program(serverIn, serverOut, serverErr);
        Task<int> serverTask = serverApp.RunAsync(new[] { TestPort.ToString() });

        await WaitUntilPortOpenAsync(TestPort);

        using (var client = new TcpClient())
        {
            await client.ConnectAsync(IPAddress.Loopback, TestPort);
        }

        int code = await serverTask;

        Assert.AreEqual(0, code);
        StringAssert.Contains(serverOut.ToString(), "Client disconnected.");
        StringAssert.Contains(serverOut.ToString(), "Chat ended.");
    }

    /// <summary>
    /// Waits until a TCP port becomes connectable on loopback or fails after a short deadline.
    /// </summary>
    /// <param name="port">Port to probe.</param>
    /// <returns>A task representing the asynchronous wait.</returns>
    private static async Task WaitUntilPortOpenAsync(int port)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(2);

        while (DateTime.UtcNow < deadline)
        {
            if (await IsPortOpenAsync(port))
            {
                return;
            }

            await Task.Delay(30);
        }

        Assert.Fail($"Server did not start listening on port {port} in time.");
    }

    /// <summary>
    /// Checks whether a port is currently accepting TCP connections on loopback.
    /// </summary>
    /// <param name="port">Port to check.</param>
    /// <returns>True if connect succeeds; otherwise false.</returns>
    private static async Task<bool> IsPortOpenAsync(int port)
    {
        try
        {
            using var probe = new TcpClient();
            await probe.ConnectAsync(IPAddress.Loopback, port);
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}
