using codecrafters_redis.Commands;

namespace codecrafters_redis.Replication;

internal class ReplicaManager(Settings settings, Processor processor)
{
    private ReplicaClient? _replicationClient;
    private int _offset = 0;
    
    public async Task ConnectToMaster()
    {
        if (settings.Replication.SlaveReplicaSettings == null) return;

        try
        {
            WriteLine($"Connecting to master {settings.Replication.SlaveReplicaSettings.MasterHost}:{settings.Replication.SlaveReplicaSettings.MasterPort}");
            _replicationClient = new ReplicaClient(settings);
            
            await _replicationClient.Ping();
            await _replicationClient.ConfListeningPort(settings.Runtime.Port);
            await _replicationClient.ConfCapabilities();
            await _replicationClient.PSync("?", -1);
            
            _ = _replicationClient!.WaitForCommandsAsync();
            _ = ProcessCommandsAsync();
        }
        catch (Exception e)
        {
            WriteLine($"Unable to ConnectToMaster: {e}");
        }
    }

    public async Task SendAckOffset()
    {
        await _replicationClient!.SendAckResponse(_offset);
    }

    private Task ProcessCommandsAsync()
    {
        return Task.Run(async () =>
        {
            foreach (var payload in _replicationClient!.MasterCommandQueue.GetConsumingEnumerable())
            {
                try
                {
                    $"Received command payload: {payload}".WriteLineEncoded();
                    await HandleCommand(payload);
                }
                catch (Exception e)
                {
                    WriteLine($"Unable to Process command from the master: {e}");
                }
            }
        });
    }

    private async Task HandleCommand(string payload)
    {
        var command = Command.Parse(payload);

        if (command.Type == CommandType.ReplConf && command.Arguments[0].ToUpperInvariant() == "GETACK")
        {
            await _replicationClient!.SendAckResponse(_offset);
        }
        else
        {
            await processor.Handle(payload, _replicationClient!.ClientConnection);
        }
        
        _offset += payload.Length;
    }
}