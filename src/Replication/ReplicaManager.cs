namespace codecrafters_redis.Replication;

internal class ReplicaManager(Settings settings, CommandHandler commandHandler)
{
    private ReplicaClient? _replicationClient;
    public Task? CommandWaiterTask { get; private set; }
    
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
            
            CommandWaiterTask = Task.Run(async () => await WaitForCommandsAsync());
        }
        catch (Exception e)
        {
            WriteLine($"Unable to ConnectToMaster: {e}");
        }
    }

    private async Task WaitForCommandsAsync()
    {
        await foreach (var commandPayload in _replicationClient!.WaitForCommandsAsync())
        {
            var command = commandPayload.Parse();
            commandHandler.Handle(command);
        }
    }
}