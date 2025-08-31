using codecrafters_redis.UserSettings;

namespace codecrafters_redis.Replication;

public class ReplicationManager(Settings settings)
{
    private ReplicationClient? _replicationClient;
    public async Task ConnectToMaster()
    {
        if (settings.Replication.SlaveReplicaSettings == null) return;

        try
        {
            _replicationClient = new ReplicationClient(settings);
            await _replicationClient.Ping();
            await _replicationClient.SendListeningPort(settings.Runtime.Port);
            await _replicationClient.SendCapabilities();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unable to ConnectToMaster: {e}");
        }
    }
}