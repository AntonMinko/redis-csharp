using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis;

public interface IWorker
{
    Task HandleConnectionAsync(Socket socket);
}

internal class TcpConnectionWorker(CommandHandler commandHandler, Settings settings, MasterManager masterManager): IWorker
{
    public async Task HandleConnectionAsync(Socket socket)
    {
        try
        {
            while (socket.Connected)
            {
                WriteLine("Waiting for request...");
                var buffer = new byte[1024];
                var received = await socket.ReceiveAsync(buffer, SocketFlags.None);
                if (received == 0)
                {
                    WriteLine("Socket disconnected");
                    break;
                }

                var requestPayload = Encoding.UTF8.GetString(buffer, 0, received);
                WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}. Received request: {requestPayload}");

                var command = requestPayload.Parse();
                var response = HandleCommand(socket, command);

                var value = response.Value;
                WriteLine($"Going to send response: {Encoding.UTF8.GetString(value, 0, value.Length)}");

                await socket.SendAsync(value);
                
                if (response.Success)
                {
                    masterManager.PropagateCommand(command);
                }
            }

            socket.Close();
            WriteLine("Connection closed");
        }
        catch (Exception e)
        {
            WriteLine(e);
        }
    }

    private RedisValue HandleCommand(Socket socket, string[] command)
    {
        if (command[0].ToUpperInvariant() == "PSYNC")
        {
            if (settings.Replication.Role != ReplicationRole.Master)
                return "ERR Only master can handle PSYNC commands".ToErrorString();
            
            WriteLine("Handle PSync");
            var masterReplId = settings.Replication.MasterReplicaSettings!.MasterReplId;
            var masterReplOffset = settings.Replication.MasterReplicaSettings.MasterReplOffset;
            var fullResync = $"FULLRESYNC {masterReplId} {masterReplOffset}".ToSimpleString();
            
            masterManager.InitReplicaConnection(socket, fullResync);
            
            return RedisValue.EmptyResponse;
        }

        return commandHandler.Handle(command);
    }
}