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
                await ProcessCommandAsync(socket, command);
            }
            
            socket.Close();
            Console.WriteLine("Connection closed");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private async Task ProcessCommandAsync(Socket socket, List<string> command)
    {
        switch (command[0].ToUpperInvariant())
        {
            case "PING":
                await socket.SendAsync(SimpleString("PONG"), SocketFlags.None);
                break;
            case "ECHO":
                await socket.SendAsync(SimpleString(command[1]), SocketFlags.None);
                break;
            default:
                Console.WriteLine("Unknown command: " + String.Join(" ", command));
                await socket.SendAsync(ErrorString($"Unknown command {command[0]}"), SocketFlags.None);
                break;
        }
    }

    private byte[] SimpleString(string s)
    {
        return Encoding.UTF8.GetBytes($"+{s}\r\n");
    }

    private byte[] ErrorString(string message)
    {
        return Encoding.UTF8.GetBytes($"-{message}\r\n");
    }
    
    public void Dispose()
    {
        socket.Dispose();
    }
}