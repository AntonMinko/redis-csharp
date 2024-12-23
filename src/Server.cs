using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis;

Console.WriteLine("Logs from your program will appear here!");

TcpListener server = new TcpListener(IPAddress.Any, 6379);

while (true)
{
    try
    {
        server.Start(10);
        var socket = await server.AcceptSocketAsync(); // wait for client
        Console.WriteLine("Connected new client!");
        var handler = new TcpConnectionWorker(socket);
        var thread = new Thread(() => handler.HandleConnectionAsync());
        thread.Start();
    }
    finally
    {
        server.Stop();
    }
}

