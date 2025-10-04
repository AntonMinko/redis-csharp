namespace codecrafters_redis.Replication;

internal class ReplicaManager(Settings settings, CommandHandler commandHandler)
{
    private ReplicaClient? _replicationClient;
    private int _offset = 0;
    public Task? CommandWaiterTask { get; private set; }
    public Task? CommandProcessorTask { get; private set; }
    
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
            
            CommandWaiterTask = _replicationClient!.WaitForCommandsAsync();
            CommandProcessorTask = ProcessCommandsAsync();
        }
        catch (Exception e)
        {
            WriteLine($"Unable to ConnectToMaster: {e}");
        }
    }

    private Task ProcessCommandsAsync()
    {
        return Task.Run(async () =>
        {
            foreach (var payload in _replicationClient!.MasterCommandQueue.GetConsumingEnumerable())
            {
                try
                {
                    $"Received command payload: {payload.Replace("\r\n", "\\r\\n")}".WriteLineEncoded();
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
        var command = payload.Parse();

        if (command.Length == 0)
        {
            _offset += payload.Length;
            return;
        }

        switch (command[0].ToUpperInvariant())
        {
            case "REPLCONF":
                await _replicationClient!.SendAckResponse(_offset);
                break;
            default:
                commandHandler.Handle(command);
                break;
        }
        _offset += payload.Length;
    }
}