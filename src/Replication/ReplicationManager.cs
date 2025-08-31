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