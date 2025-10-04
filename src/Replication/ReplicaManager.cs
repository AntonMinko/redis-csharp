namespace codecrafters_redis.Replication;

internal class ReplicaManager(Settings settings, CommandHandler commandHandler)
{
    private ReplicaClient? _replicationClient;
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
        return Task.Run(() =>
        {
            foreach (var commandString in _replicationClient!.MasterCommandQueue.GetConsumingEnumerable())
            {
                try
                {
                    $"Received command payload: {commandString.Replace("\r\n", "\\r\\n")}".WriteLineEncoded();
                    var command = commandString.Parse();
                    commandHandler.Handle(command);
                }
                catch (Exception e)
                {
                    WriteLine($"Unable to Process command from the master: {e}");
                }
            }
        });
    }
}