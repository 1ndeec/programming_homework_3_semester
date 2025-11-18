// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MyOwnServer_Test;

using MyOwnServer;

/// <summary>
/// Test class for SimpleClient and SimpleServer.
/// </summary>
[TestClass]
public sealed class ServerClientTest
{
    /// <summary>
    /// Tests that the client reports a missing file using the Get command.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task Get_FileNotFound()
    {
        var output = new StringWriter();
        Console.SetOut(output);

        SimpleServer server = new SimpleServer(26323);
        var bin = server.StartAsync();
        await Task.Delay(50);

        var client = new SimpleClient(26323);
        await client.Get("wrongtest");
        client.Close();

        var text = output.ToString();

        Assert.IsTrue(text.Equals("File not found\r\n"));
    }

    /// <summary>
    /// Tests that the root directory of DataForTest is listed correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task List_RootDirectory()
    {
        var output = new StringWriter();
        Console.SetOut(output);

        var server = new SimpleServer(26323);
        var bin = server.StartAsync();
        await Task.Delay(50);

        var client = new SimpleClient(26323);
        await client.List("DataForTest");
        client.Close();

        var text = output.ToString();

        Assert.AreEqual("directory1 true\r\ndirectory2 true\r\nfile1.txt false\r\nfile2.txt false\r\nfile3.txt false\r\n", text);
    }

    /// <summary>
    /// Tests that the contents of DataForTest/directory2 are listed correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task List_Directory2()
    {
        var output = new StringWriter();
        Console.SetOut(output);

        var server = new SimpleServer(26323);
        var bin = server.StartAsync();
        await Task.Delay(50);

        var client = new SimpleClient(26323);
        await client.List("DataForTest/directory2");
        client.Close();

        var text = output.ToString();

        Assert.AreEqual("directory3.txt true\r\nfile4.txt false\r\n", text);
    }

    /// <summary>
    /// Tests that Get retrieves the correct content of file1.txt.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task Get_File1()
    {
        var output = new StringWriter();
        Console.SetOut(output);

        var server = new SimpleServer(26323);
        var bin = server.StartAsync();
        await Task.Delay(50);

        var client = new SimpleClient(26323);
        await client.Get("DataForTest/file1.txt");
        client.Close();

        var text = output.ToString();

        Assert.AreEqual("14 data of file 1\n", text);
    }

    /// <summary>
    /// Tests that listing a non-existing directory prints an error message.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task List_DirectoryNotFound()
    {
        var output = new StringWriter();
        Console.SetOut(output);

        var server = new SimpleServer(26323);
        var bin = server.StartAsync();
        await Task.Delay(50);

        var client = new SimpleClient(26323);
        await client.List("DataForTest/no_such_dir");
        client.Close();

        var text = output.ToString();
        Assert.AreEqual("Directory not found\r\n", text);
    }
}