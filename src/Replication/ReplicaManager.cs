using codecrafters_redis.UserSettings;

namespace codecrafters_redis.Replication;

public class ReplicaManager(Settings settings)
{
    private ReplicaClient? _replicationClient;
    public async Task ConnectToMaster()
    {
        if (settings.Replication.SlaveReplicaSettings == null) return;

        try
        {
            Console.WriteLine($"Connecting to master {settings.Replication.SlaveReplicaSettings.MasterHost}:{settings.Replication.SlaveReplicaSettings.MasterPort}");
            _replicationClient = new ReplicaClient(settings);
            await _replicationClient.Ping();
            await _replicationClient.ConfListeningPort(settings.Runtime.Port);
            await _replicationClient.ConfCapabilities();
            var masterSettings = await _replicationClient.PSync("?", -1);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unable to ConnectToMaster: {e}");
        }
    }
}