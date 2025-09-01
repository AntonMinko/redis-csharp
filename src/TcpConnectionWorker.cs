using System.Net.Sockets;
using System.Text;
using System.Text.Unicode;
using codecrafters_redis.Replication;
using codecrafters_redis.UserSettings;

namespace codecrafters_redis;

public interface IWorker
{
    Task HandleConnectionAsync(Socket socket);
}

internal class TcpConnectionWorker(CommandHandler commandHandler, MasterManager masterManager): IWorker
{
    public async Task HandleConnectionAsync(Socket socket)
    {
        try
        {
            while (socket.Connected)
            {
                var buffer = new byte[1024];
                var received = await socket.ReceiveAsync(buffer);
                if (received == 0)
                {
                    Console.WriteLine("Socket disconnected");
                    break;
                }

                var requestPayload = Encoding.UTF8.GetString(buffer, 0, received);
                Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}. Received request: {requestPayload}");

                var command = requestPayload.Parse();
                var response = commandHandler.Handle(command, socket);
                Console.WriteLine($"Going to send response: {Encoding.UTF8.GetString(response, 0, response.Length)}");

                await socket.SendAsync(response);
                
                if (response.FirstOrDefault() != '-')
                {
                    await masterManager.PropagateCommand(command);
                }
            }

            socket.Close();
            Console.WriteLine("Connection closed");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}