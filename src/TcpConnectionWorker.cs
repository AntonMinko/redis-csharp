using System.Net.Sockets;
using System.Text;
using System.Text.Unicode;

namespace codecrafters_redis;

public class TcpConnectionWorker(Socket socket) : IDisposable
{
    public async Task HandleConnectionAsync()
    {
        try
        {
            while (socket.Connected) {
                var buffer = new byte[1024];
                var received = await socket.ReceiveAsync(buffer, SocketFlags.None);
                if (received == 0)
                {
                    Console.WriteLine("Socket disconnected");
                    break;
                }
                var requestPayload = Encoding.UTF8.GetString(buffer, 0, received);
                Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}. Received request: {requestPayload}");
                
                var command = requestPayload.Parse();
                var response = new CommandHandler().Handle(command);
                
                await socket.SendAsync(response, SocketFlags.None);
            }
            
            socket.Close();
            Console.WriteLine("Connection closed");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public void Dispose()
    {
        socket.Dispose();
    }
}