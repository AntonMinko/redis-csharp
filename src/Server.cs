using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();
try
{
    var socket = await server.AcceptSocketAsync(); // wait for client
    while (socket.Connected) {
        var buffer = new byte[1024];
        var received = await socket.ReceiveAsync(buffer, SocketFlags.None);
        var requestPayload = Encoding.UTF8.GetString(buffer, 0, received);
        Console.WriteLine($"Received request: {requestPayload}");

        await socket.SendAsync(Encoding.UTF8.GetBytes("+PONG\r\n"), SocketFlags.None);
    }
}
finally
{
    server.Stop();
}

