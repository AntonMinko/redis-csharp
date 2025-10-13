using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Commands;

namespace codecrafters_redis;

internal interface IWorker
{
    Task<bool> HandleConnectionAsync(ClientConnection connection);
}

internal class TcpConnectionWorker(Processor processor, Settings settings, MasterManager masterManager): IWorker
{
    public async Task<bool> HandleConnectionAsync(ClientConnection connection)
    {
        try
        {
            var buffer = new byte[1024];
            var socket = connection.Socket;
            while (socket.Connected)
            {
                $"Connection Id {connection.Id}. Waiting for request...".WriteLineEncoded();
                var received = await socket.ReceiveAsync(buffer, SocketFlags.None);
                if (received == 0)
                {
                    $"Connection Id {connection.Id}. Socket disconnected".WriteLineEncoded();
                    break;
                }

                var requestPayload = Encoding.UTF8.GetString(buffer, 0, received);
                $"Connection Id {connection.Id}. Received request: {requestPayload}".WriteLineEncoded();

                var response = await processor.Handle(requestPayload, connection);
                if (response.Type == ReplicaConnection || response.Type == RedisTypes.Void)
                {
                    continue;
                }

                var value = response.Value;
                $"Connection Id {connection.Id}. Sending response: {Encoding.UTF8.GetString(value, 0, value.Length)}".WriteLineEncoded();

                await socket.SendAsync(value);
            }

            socket.Close();
            $"Connection Id {connection.Id}. Connection closed".WriteLineEncoded();
        }
        catch (Exception e)
        {
            WriteLine(e);
        }

        return true;
    }
}