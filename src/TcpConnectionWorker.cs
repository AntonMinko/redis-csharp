using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis;

public class TcpConnectionWorker(Socket socket)
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

                await socket.SendAsync("+PONG\r\n"u8.ToArray(), SocketFlags.None);
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