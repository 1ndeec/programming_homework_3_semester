// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MyOwnServer_Test;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyOwnServer;

/// <summary>
/// Tests for <see cref="SimpleClient"/> and <see cref="SimpleServer"/> using the real DataForTest structure.
/// </summary>
[TestClass]
public sealed class ServerClientTest
{
    /// <summary>
    /// Tests that listing DataForTest returns directory2 plus files 1..3 (order-independent).
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task List_DataForTest_Root()
    {
        int port = GetFreePort();
        using var cts = new CancellationTokenSource();

        var server = new SimpleServer(port);
        var serverTask = server.StartAsync(cts.Token);

        await Task.Delay(50);

        using var client = new SimpleClient(port);
        var items = await client.AsyncList("DataForTest", cts.Token);

        var expected = new HashSet<string>
        {
            "directory1 true",
            "directory2 true",
            "file1.txt false",
            "file2.txt false",
            "file3.txt false",
        };

        Assert.HasCount(expected.Count, items);
        Assert.IsTrue(expected.SetEquals(items));

        await StopServerAsync(cts, serverTask);
    }

    /// <summary>
    /// Tests that listing DataForTest/directory2 returns file4.txt (order-independent).
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task List_DataForTest_Directory2()
    {
        int port = GetFreePort();
        using var cts = new CancellationTokenSource();

        var server = new SimpleServer(port);
        var serverTask = server.StartAsync(cts.Token);

        await Task.Delay(50);

        using var client = new SimpleClient(port);
        var items = await client.AsyncList("DataForTest/directory2", cts.Token);

        var expected = new HashSet<string>
        {
            "directory3 true",
            "file4.txt false",
        };

        Assert.HasCount(expected.Count, items);
        Assert.IsTrue(expected.SetEquals(items));

        await StopServerAsync(cts, serverTask);
    }

    /// <summary>
    /// Tests that Get returns correct size and content for DataForTest/file1.txt.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task Get_File1_ReturnsExpectedContent()
    {
        await Get_FileN_ReturnsExpectedContent(1);
    }

    /// <summary>
    /// Tests that Get returns correct size and content for DataForTest/file2.txt.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task Get_File2_ReturnsExpectedContent()
    {
        await Get_FileN_ReturnsExpectedContent(2);
    }

    /// <summary>
    /// Tests that Get returns correct size and content for DataForTest/file3.txt.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task Get_File3_ReturnsExpectedContent()
    {
        await Get_FileN_ReturnsExpectedContent(3);
    }

    /// <summary>
    /// Tests that requesting a missing file returns (-1, empty string).
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task Get_FileNotFound_ReturnsMinusOne()
    {
        int port = GetFreePort();
        using var cts = new CancellationTokenSource();

        var server = new SimpleServer(port);
        var serverTask = server.StartAsync(cts.Token);

        await Task.Delay(50);

        using var client = new SimpleClient(port);
        var (size, content) = await client.AsyncGet("DataForTest/no_such_file.txt", cts.Token);

        Assert.AreEqual(-1, size);
        Assert.AreEqual(string.Empty, content);

        await StopServerAsync(cts, serverTask);
    }

    /// <summary>
    /// Tests that two clients can connect to one server and perform requests independently.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    [Timeout(15000, CooperativeCancellation = true)]
    public async Task TwoClients_OneServer_WorkIndependently()
    {
        int port = GetFreePort();
        using var cts = new CancellationTokenSource();

        var server = new SimpleServer(port);
        var serverTask = server.StartAsync(cts.Token);

        await Task.Delay(50);

        using var client1 = new SimpleClient(port);
        using var client2 = new SimpleClient(port);

        var t1 = client1.AsyncGet("DataForTest/file1.txt", cts.Token);
        var t2 = client2.AsyncGet("DataForTest/file2.txt", cts.Token);

        await Task.WhenAll(t1, t2);

        var (size1, content1) = await t1;
        var (size2, content2) = await t2;

        Assert.AreEqual("data of file 1".Length, size1);
        Assert.AreEqual("data of file 1", content1);

        Assert.AreEqual("data of file 2".Length, size2);
        Assert.AreEqual("data of file 2", content2);

        await StopServerAsync(cts, serverTask);
    }

    private static async Task Get_FileN_ReturnsExpectedContent(int n)
    {
        int port = GetFreePort();
        using var cts = new CancellationTokenSource();

        var server = new SimpleServer(port);
        var serverTask = server.StartAsync(cts.Token);

        await Task.Delay(50);

        using var client = new SimpleClient(port);

        string path = $"DataForTest/file{n}.txt";
        string expectedText = $"data of file {n}";
        int expectedSize = expectedText.Length;

        var (size, content) = await client.AsyncGet(path, cts.Token);

        Assert.AreEqual(expectedSize, size);
        Assert.AreEqual(expectedText, content);

        await StopServerAsync(cts, serverTask);
    }

    /// <summary>
    /// Finds an available local TCP port.
    /// </summary>
    /// <returns>A free TCP port number.</returns>
    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>
    /// Cancels the server and waits for it to stop.
    /// </summary>
    /// <param name="cts">Cancellation token source used to stop the server.</param>
    /// <param name="serverTask">The task returned by <see cref="SimpleServer.StartAsync"/>.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task StopServerAsync(CancellationTokenSource cts, Task serverTask)
    {
        cts.Cancel();

        var completed = await Task.WhenAny(serverTask, Task.Delay(3000));
        if (completed != serverTask)
        {
            Assert.Fail("Server did not stop within the timeout.");
        }

        await serverTask;
    }
}
