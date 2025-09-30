using System.Collections.Concurrent;
using System.Net.Sockets;

namespace codecrafters_redis.Replication;

public class MasterManager
{
    record Replica(Guid Id, Socket Socket);
    
    private const string EmptyRdbFile = "UkVESVMwMDEx+glyZWRpcy12ZXIFNy4yLjD6CnJlZGlzLWJpdHPAQPoFY3RpbWXCbQi8ZfoIdXNlZC1tZW3CsMQQAPoIYW9mLWJhc2XAAP/wbjv+wP9aog==";
    
    private ConcurrentBag<Replica> _replicas = new();
    
    public void InitReplicaConnection(Socket socket, RedisValue pSyncResponse)
    {
        var emptyRdbFileBytes = Convert.FromBase64String(EmptyRdbFile).ToBinaryContent();
        socket.Send(pSyncResponse.Value);
        socket.Send(emptyRdbFileBytes.Value);
        
        _replicas.Add(new Replica(Guid.NewGuid(), socket));
    }

    public void PropagateCommand(string[] command)
    {
        if (_replicas.Count == 0) return;

        var commandType = command[0].ToUpperInvariant();
        switch (commandType)
        {
            case "SET":
                WriteLine($"Sending {commandType} command to replicas");
                Broadcast(command);
                break;
            default:
                WriteLine($"Command {commandType} shouldn't be replicated");
                break;
        }
    }
    
    private void Broadcast(string[] command)
    {
        var commandBytes = command.ToBulkStringArray().Value;
        foreach (var replica in _replicas)
        {
            if (replica.Socket.Connected)
            {
                replica.Socket.SendAsync(commandBytes);
            }
            else
            {
                var current = replica;
                _replicas.TryTake(out current);
            }
        }
    }
}