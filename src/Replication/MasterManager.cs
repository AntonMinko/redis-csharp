using System.Collections.Concurrent;
using System.Net.Sockets;

namespace codecrafters_redis.Replication;

public class MasterManager(Settings settings)
{
    private const string EmptyRdbFile = "UkVESVMwMDEx+glyZWRpcy12ZXIFNy4yLjD6CnJlZGlzLWJpdHPAQPoFY3RpbWXCbQi8ZfoIdXNlZC1tZW3CsMQQAPoIYW9mLWJhc2XAAP/wbjv+wP9aog==";
    
    private readonly ConcurrentBag<Replica> _replicas = new();
    private readonly ReplicationLog _replicationLog = new();
    private Task _replicationTask;
    private const int ReplicationIntervalMs = 5;

    public void StartReplication()
    {
        if (settings.Replication.Role != ReplicationRole.Master) return;
        
        _replicationTask = Task.Run(async () =>
        {
            while (true)
            {
                foreach (var replica in _replicas)
                {
                    foreach (var command in _replicationLog.GetCommandsToReplicate(replica.Offset))
                    {
                        await replica.SendAsync(command);
                    }
                }
                await Task.Delay(ReplicationIntervalMs);
            }
        });
    }
    
    public void InitReplicaConnection(Socket socket, RedisValue pSyncResponse)
    {
        var emptyRdbFileBytes = Convert.FromBase64String(EmptyRdbFile).ToBinaryContent();
        socket.Send(pSyncResponse.Value);
        socket.Send(emptyRdbFileBytes.Value);
        
        _replicas.Add(new Replica(Guid.NewGuid(), socket));
    }

    public long PropagateCommand(string[] command)
    {
        var commandType = command[0].ToUpperInvariant();
        switch (commandType)
        {
            case "SET":
                WriteLine($"Sending {commandType} command to replicas");
                var commandBytes = command.ToBulkStringArray().Value;
                return _replicationLog.Append(commandBytes);
            default:
                WriteLine($"Command {commandType} shouldn't be replicated");
                return _replicationLog.Offset;
        }
    }

    public int CountReplicasWithOffset(long offset) => _replicas.Count(replica => replica.Offset >= offset);
}