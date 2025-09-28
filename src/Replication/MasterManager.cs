using System.Net.Sockets;

namespace codecrafters_redis.Replication;

public class MasterManager
{
    private const string EmptyRdbFile = "UkVESVMwMDEx+glyZWRpcy12ZXIFNy4yLjD6CnJlZGlzLWJpdHPAQPoFY3RpbWXCbQi8ZfoIdXNlZC1tZW3CsMQQAPoIYW9mLWJhc2XAAP/wbjv+wP9aog==";

    private Socket? _replicaSocket;
    
    public void InitReplicaConnection(Socket socket, RedisValue pSyncResponse)
    {
        _replicaSocket = socket;
        
        var emptyRdbFileBytes = Convert.FromBase64String(EmptyRdbFile).ToBinaryContent();
        _replicaSocket.Send(pSyncResponse.Value);
        _replicaSocket.Send(emptyRdbFileBytes.Value);
    }

    public void PropagateCommand(string[] command)
    {
        if (_replicaSocket == null || _replicaSocket.Connected == false) return;

        var commandType = command[0].ToUpperInvariant();
        switch (commandType)
        {
            case "SET":
                WriteLine($"Sending {commandType} command to replica");
                SendAsync(command);
                break;
            default:
                WriteLine($"Command {commandType} shouldn't be replicated");
                break;
        }
    }
    
    private void SendAsync(string[] command)
    {
        _replicaSocket?.SendAsync(command.ToBulkStringArray().Value);
    }
}