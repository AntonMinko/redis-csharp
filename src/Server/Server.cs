using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;

namespace codecrafters_redis.Server;

public class Server(IServiceProvider serviceProvider, Settings settings)
{
    internal IDictionary<int, ClientConnection> Clients { get; } = new Dictionary<int, ClientConnection>();
    private int _nextId = 0;
    
    internal async Task StartAndListen()
    {
        int port = settings.Runtime.Port;
        TcpListener server = new TcpListener(IPAddress.Any, port);
        WriteLine($"Server listening on port {port}");

        try
        {
            server.Start(10);
            while (true)
            {
                var socket = await server.AcceptSocketAsync(); // wait for client
                WriteLine("Connected new client!");

                _ = Task.Run(async () => await HandleConnectionAsync(socket));
            }
        }
        finally
        {
            WriteLine($"Stopping server");
            server.Stop();
        }
    }

    private async Task HandleConnectionAsync(Socket socket)
    {
        int connectionId = _nextId++;
        var connection = new ClientConnection(connectionId, socket);
        Clients.Add(connectionId, connection);
        
        var worker = serviceProvider.GetRequiredService<IWorker>();
        var disposeSocket = await worker.HandleConnectionAsync(connection);
        if (disposeSocket) socket.Dispose();
    }
}