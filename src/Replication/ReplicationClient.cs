using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using codecrafters_redis.Helpers;
using codecrafters_redis.UserSettings;

namespace codecrafters_redis.Replication;

public class ReplicationClient(Settings settings)
{
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);
    private readonly byte[] _buffer = new byte[1024];
    private readonly TcpClient _connection = new(settings.Replication.SlaveReplicaSettings!.MasterHost, settings.Replication.SlaveReplicaSettings.MasterPort);

    public async Task Ping()
    {
        var response = await SendAndReceiveCommand(new[] { "PING" }.ToBulkStringArray());
        Console.WriteLine($"Ping response: {response}");
    }

    public async Task ConfListeningPort(int port)
    {
        await SendWithConfirmationCommand(new[] {"REPLCONF", "listening-port", port.ToString()}.ToBulkStringArray());
    }

    public async Task ConfCapabilities()
    {
        await SendWithConfirmationCommand(new[] {"REPLCONF", "capa", "psync2"}.ToBulkStringArray());
    }

    public async Task<string> PSync(string masterReplicationId, int offset)
    {
        return await SendAndReceiveCommand(new[] {"PSYNC", masterReplicationId, offset.ToString()}.ToBulkStringArray());
    }

    private async Task<string> SendAndReceiveCommand(byte[] message)
    {
        if (!_connection.Connected) throw new ChannelClosedException();
        
        var stream = _connection.GetStream();
        await stream.WriteAsync(message);
        
        using var cts = new CancellationTokenSource(_timeout);
        _buffer.Initialize();
        int received = await stream.ReadAsync(_buffer, cts.Token);
        var payload = Encoding.UTF8.GetString(_buffer, 0, received);
        return payload;
    }

    private async Task SendWithConfirmationCommand(byte[] message, string confirmation = RedisTypes.OkString)
    {
        try
        {
            var response = await SendAndReceiveCommand(message);
            if (response != confirmation)
            {
                throw new IOException($"Invalid confirmation received: {response}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new IOException($"Failed to send a message with confirmation: {e}");
        }
    }
}