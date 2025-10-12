using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis;

public interface IWorker
{
    Task<bool> HandleConnectionAsync(Socket socket);
}

internal class TcpConnectionWorker(CommandHandler commandHandler, Settings settings, MasterManager masterManager): IWorker
{
    private const int WaitPollIntervalMs = 5;
    
    private long _lastCommandOffset = 0;
    
    public async Task<bool> HandleConnectionAsync(Socket socket)
    {
        try
        {
            var buffer = new byte[1024];
            while (socket.Connected)
            {
                WriteLine("Waiting for request...");
                var received = await socket.ReceiveAsync(buffer, SocketFlags.None);
                WriteLine($"Received {received} bytes");
                if (received == 0)
                {
                    WriteLine("Socket disconnected");
                    break;
                }

                var requestPayload = Encoding.UTF8.GetString(buffer, 0, received);
                $"Thread {Thread.CurrentThread.ManagedThreadId}. Received request: {requestPayload}".WriteLineEncoded();

                var command = requestPayload.Parse();
                var response = await HandleCommand(socket, command);
                if (response.Type == ReplicaConnection || response.Type == RedisTypes.Void)
                {
                    $"Sending no response to command {command}".WriteLineEncoded();
                    continue;
                }

                var value = response.Value;
                $"Going to send response: {Encoding.UTF8.GetString(value, 0, value.Length)}".WriteLineEncoded();

                await socket.SendAsync(value);
                
                if (response.Success)
                {
                    _lastCommandOffset = masterManager.PropagateCommand(command);
                    $"Last command offset: {_lastCommandOffset}".WriteLineEncoded();
                }
            }

            socket.Close();
            WriteLine("Connection closed");
        }
        catch (Exception e)
        {
            WriteLine(e);
        }

        return true;
    }

    private async Task<RedisValue> HandleCommand(Socket socket, string[] command)
    {
        var commandType = command[0].ToUpperInvariant();

        // handle replication commands here
        switch (commandType)
        {
            case "REPLCONF":
                return HandleReplConf(command, socket);
            case "PSYNC":
                return HandlePSyncCommand(socket);
            case "WAIT":
                return await HandleWaitCommand(command);
        }

        return commandHandler.Handle(command);
    }

    private RedisValue HandleReplConf(string[] command, Socket socket)
    {
        if (command.Length < 3) return "REPLCONF should contain two arguments".ToErrorString();
        
        string commandSubtype = command[1];
        switch (commandSubtype)
        {
            case "ACK":
                long ackOffset = long.Parse(command[2]);
                masterManager.SetReplicaAckOffset(ackOffset, socket);
                return NoResponse; 
            default:
                return OkBytes;
        }
    }
    
    private async Task<RedisValue> HandleWaitCommand(string[] command)
    {
        if (settings.Replication.Role != ReplicationRole.Master)
            return "Only master can handle the WAIT command".ToErrorString();
        
        int expectReplicas = int.Parse(command[1]);
        int timeoutMs = int.Parse(command[2]);
        $"Handling wait command. Expected replicas: {expectReplicas}, timeout: {timeoutMs} ms, Last command offset: {_lastCommandOffset}".WriteLineEncoded();
        
        int upToDateReplicas = masterManager.CountReplicasWithAckOffset(_lastCommandOffset);
        if (upToDateReplicas < expectReplicas)
        {
            var delayTask = Task.Delay(timeoutMs);
            await masterManager.UpdateReplicasOffsets();

            await delayTask;
        }
        
        // on timeout, return the actual number of sync replicas
        upToDateReplicas = masterManager.CountReplicasWithAckOffset(_lastCommandOffset);
        return upToDateReplicas.ToIntegerString();
    }

    private RedisValue HandlePSyncCommand(Socket socket)
    {
        if (settings.Replication.Role != ReplicationRole.Master)
            return "ERR Only master can handle PSYNC commands".ToErrorString();
            
        WriteLine("Handle PSync");
        var masterReplId = settings.Replication.MasterReplicaSettings!.MasterReplId;
        var masterReplOffset = settings.Replication.MasterReplicaSettings.MasterReplOffset;
        var fullResync = $"FULLRESYNC {masterReplId} {masterReplOffset}".ToSimpleString();
            
        masterManager.InitReplicaConnection(socket, fullResync);
            
        return ReplicaConnectionResponse;
    }
}