using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;

namespace codecrafters_redis;

public class Server(IServiceProvider serviceProvider, Settings settings)
{
    internal async Task StartAndListen()
    {
        WriteLine("Logs from your program will appear here!");

        int port = settings.Runtime.Port;
        TcpListener server = new TcpListener(IPAddress.Any, port);
        WriteLine($"Listening on port {port}");

        try
        {
            server.Start(10);
            while (true)
            {
                var socket = await server.AcceptSocketAsync(); // wait for client
                WriteLine("Connected new client!");
                
                var worker = serviceProvider.GetRequiredService<IWorker>();

                _ = Task.Run(async () => await HandleConnectionAsync(socket, worker));
            }
        }
        finally
        {
            WriteLine($"Stopping server");
            server.Stop();
        }
    }

    private static async Task HandleConnectionAsync(Socket socket, IWorker worker)
    {
        var disposeSocket = await worker.HandleConnectionAsync(socket);
        if (disposeSocket) socket.Dispose();
    }
}