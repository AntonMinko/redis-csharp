using codecrafters_redis.Commands;

namespace codecrafters_redis.Replication;

internal class MasterManager(Settings settings, Server server)
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
    
    public void InitReplicaConnection(ClientConnection connection, RedisValue pSyncResponse)
    {
        var emptyRdbFileBytes = Convert.FromBase64String(EmptyRdbFile).ToBinaryContent();
        
        var socket = connection.Socket;
        socket.Send(pSyncResponse.Value);
        socket.Send(emptyRdbFileBytes.Value);

        var replica = new Replica(connection.Id, socket);
        _replicas.Add(replica);
        $"Connected replica with id {replica.Id}".WriteLineEncoded();
    }

    public long PropagateCommand(Command command)
    {
        WriteLine($"Sending {command.Type} command to replicas");
        var commandBytes = command.ToBulkStringArray().Value;
        return _replicationLog.Append(commandBytes);
    }

    public int CountReplicasWithAckOffset(long offset) => _replicas.Count(replica => replica.AckOffset >= offset);

    public async Task UpdateReplicasOffsets()
    {
        var tasks = _replicas.Select(replica => replica.GetAckOffset(_replicationLog.Offset));
        await Task.WhenAll(tasks);
    }

    public void SetReplicaAckOffset(long offset, ClientConnection connection)
    {
        var replica = _replicas.FirstOrDefault(replica => replica.Id == connection.Id);
        if (replica == null)
        {
            $"Unable to find replica by connectionId {connection.Id}".WriteLineEncoded();
            return;
        }
        
        replica.AckOffset = offset;
        $"Set AckOffset={replica.AckOffset} for replica {replica.Id}".WriteLineEncoded();
    }
}