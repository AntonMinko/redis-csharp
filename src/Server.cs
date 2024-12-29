using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;

namespace codecrafters_redis;

public class Server(IServiceProvider serviceProvider)
{
    internal async Task StartAndListen()
    {
        Console.WriteLine("Logs from your program will appear here!");

        TcpListener server = new TcpListener(IPAddress.Any, 6379);

        while (true)
        {
            try
            {
                server.Start(10);
                var socket = await server.AcceptSocketAsync(); // wait for client
                Console.WriteLine("Connected new client!");
                
                var worker = serviceProvider.GetRequiredService<IWorker>();

                _ = Task.Run(async () => await HandleConnectionAsync(socket, worker));
            }
            finally
            {
                server.Stop();
            }
        }
    }

    private static async Task HandleConnectionAsync(Socket socket, IWorker worker)
    {
        using (socket)
        {
            await worker.HandleConnectionAsync(socket);
        }
    }
}