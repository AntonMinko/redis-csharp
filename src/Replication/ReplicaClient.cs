using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace codecrafters_redis.Replication;

public class ReplicaClient(Settings settings)
{
    private readonly byte[] _buffer = new byte[1024];
    private readonly TcpClient _connection = new(settings.Replication.SlaveReplicaSettings!.MasterHost, settings.Replication.SlaveReplicaSettings.MasterPort);

    public async Task Ping()
    {
        await SendAndReceiveCommand(new[] { "PING" }.ToBulkStringArray());
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

    public async Task<string> ReceiveRDB()
    {
        var buffer = new byte[1024];
        var received = await _connection.Client.ReceiveAsync(buffer, SocketFlags.None);
        if (received == 0)
        {
            WriteLine("Connection with the master disconnected");
            return string.Empty;
        }

        return Encoding.UTF8.GetString(buffer, 0, received);
    }

    public async IAsyncEnumerable<string> WaitForCommandsAsync()
    {
        while (_connection.Connected)
        {
            WriteLine("Waiting for commands from the master...");
            var buffer = new byte[1024];
            var received = await _connection.Client.ReceiveAsync(buffer, SocketFlags.None);
            if (received == 0)
            {
                WriteLine("Connection with the master disconnected");
                break;
            }

            var requestPayload = Encoding.UTF8.GetString(buffer, 0, received);
            WriteLine($"Received command from the master: {requestPayload}");
            yield return requestPayload;
        }

        _connection.Close();
        WriteLine("Connection with the master closed");
    }

    private async Task<string> SendAndReceiveCommand(RedisValue message)
    {
        if (!_connection.Connected) throw new ChannelClosedException();

        await _connection.Client.SendAsync(message.Value);
        
        _buffer.Initialize();
        int received = await _connection.Client.ReceiveAsync(_buffer);
        var payload = Encoding.UTF8.GetString(_buffer, 0, received);
        WriteLine($"Received from master: {payload}");
        return payload;
    }

    private async Task SendWithConfirmationCommand(RedisValue message, string confirmation = RedisTypesExtensions.OkString)
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
            WriteLine(e);
            throw new IOException($"Failed to send a message with confirmation: {e}");
        }
    }
}