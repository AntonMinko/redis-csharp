using System.Collections.Concurrent;
using System.Net.Sockets;

namespace codecrafters_redis.Replication;

public class MasterManager(Settings settings)
{
    private const string EmptyRdbFile = "UkVESVMwMDEx+glyZWRpcy12ZXIFNy4yLjD6CnJlZGlzLWJpdHPAQPoFY3RpbWXCbQi8ZfoIdXNlZC1tZW3CsMQQAPoIYW9mLWJhc2XAAP/wbjv+wP9aog==";
    
    private readonly List<Replica> _replicas = new();
    private readonly ReplicationLog _replicationLog = new();
    private const int ReplicationIntervalMs = 1;

    public void StartReplication()
    {
        if (settings.Replication.Role != ReplicationRole.Master) return;
        
        _ = Task.Run(async () =>
        {
            "Starting replication task...".WriteLineEncoded();
            
            while (true)
            {
                try
                {
                    foreach (var replica in _replicas)
                    {
                        var tasks = new List<Task>();
                        foreach (var command in _replicationLog.GetCommandsToReplicate(replica.Offset))
                        {
                            $"Sending replication command to replica {replica.Id}".WriteLineEncoded();
                            tasks.Add(replica.SendAsync(command));
                        }
                        await Task.WhenAll(tasks);
                    }
                    await Task.Delay(ReplicationIntervalMs);
                }
                catch (Exception e)
                {
                    $"Replication failed: {e.Message}".WriteLineEncoded();
                }
            }
        });
    }
    
    public void InitReplicaConnection(Socket socket, RedisValue pSyncResponse)
    {
        var emptyRdbFileBytes = Convert.FromBase64String(EmptyRdbFile).ToBinaryContent();
        socket.Send(pSyncResponse.Value);
        socket.Send(emptyRdbFileBytes.Value);

        var replica = new Replica(_replicas.Count, socket);
        _replicas.Add(replica);
        $"Connected replica with id {replica.Id}".WriteLineEncoded();
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

    public int CountReplicasWithAckOffset(long offset) => _replicas.Count(replica => replica.AckOffset >= offset);

    public async Task UpdateReplicasOffsets()
    {
        var tasks = _replicas.Select(replica => replica.GetAckOffset(_replicationLog.Offset));
        await Task.WhenAll(tasks);
    }

    public void SetReplicaAckOffset(long offset, Socket socket)
    {
        var replica = _replicas.FirstOrDefault(replica => replica.Socket == socket);
        if (replica == null)
        {
            $"Unable to find replica by socket {socket.Handle}".WriteLineEncoded();
            return;
        }
        
        replica.AckOffset = offset;
        $"Set AckOffset={replica.AckOffset} for replica {replica.Id}".WriteLineEncoded();
    }
}