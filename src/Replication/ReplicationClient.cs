using System.Net.Sockets;
using codecrafters_redis.Helpers;
using codecrafters_redis.UserSettings;

namespace codecrafters_redis.Replication;

public class ReplicationClient(Settings settings)
{
    private readonly TcpClient _connection = new TcpClient(settings.Replication.SlaveReplicaSettings!.MasterHost, settings.Replication.SlaveReplicaSettings.MasterPort);

    public async Task Ping()
    {
        var stream = _connection.GetStream();
        await stream.WriteAsync(new[] { "PING" }.ToBulkStringArray());
    }
}