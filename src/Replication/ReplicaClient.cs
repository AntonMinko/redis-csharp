using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace codecrafters_redis.Replication;

internal class ReplicaClient
{
    private static readonly HashSet<char> Delimiters = ['*', '+'];
    
    private readonly byte[] _buffer = new byte[1024];
    private readonly TcpClient _tcpClient;
    private readonly Socket _socket;
    internal ClientConnection ClientConnection { get; private set; }

    public ReplicaClient(Settings settings)
    {
        _tcpClient = new TcpClient(settings.Replication.SlaveReplicaSettings!.MasterHost, settings.Replication.SlaveReplicaSettings.MasterPort);
        _socket = _tcpClient.Client;
        ClientConnection = new ClientConnection(-1, _tcpClient.Client);
    }
    internal BlockingCollection<string> MasterCommandQueue { get; private set; } = new();
    
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

    public async Task PSync(string masterReplicationId, int offset)
    {
        var payload = await SendAndReceiveCommand(new[] {"PSYNC", masterReplicationId, offset.ToString()}.ToBulkStringArray());
        ProcessPSyncResponse(payload);
    }

    public async Task SendAckResponse(int offset)
    {
        var message = new[] { "REPLCONF", "ACK", offset.ToString() }.ToBulkStringArray();
        await _socket.SendAsync(message.Value);
    }

    private void ProcessPSyncResponse(string payload)
    {
        /*
         * Payload contains:
         * * PSync response as a simple string (starts with +)
         * * Optionally, RDB file content as Bulk String (starts with $)
         * * Optionally, one or more commands as Bulk String Array (starts with *)
         */
        var rdbStartIndex = payload.IndexOf('$');
        if (rdbStartIndex > 0)
        {
            var psyncResponse = payload.Substring(0, rdbStartIndex);
            $"PSync response from master: {psyncResponse}".WriteLineEncoded();
            payload = payload.Substring(rdbStartIndex);
        }
        
        var extraCommandsIndex = payload.IndexOf('*');
        if (extraCommandsIndex > 0)
        {
            var extraCommands = payload.Substring(extraCommandsIndex);
            AddCommandsToQueue(extraCommands);
            payload = payload.Substring(0, extraCommandsIndex);
        }
        
        $"RDB file from master: {payload}".WriteLineEncoded();
    }

    public async Task WaitForCommandsAsync()
    {
        while (_tcpClient.Connected)
        {
            WriteLine("Waiting for commands from the master...");
            _buffer.Initialize();
            var received = await _socket.ReceiveAsync(_buffer, SocketFlags.None);
            if (received == 0)
            {
                WriteLine("Connection with the master disconnected");
                break;
            }

            var payload = Encoding.UTF8.GetString(_buffer, 0, received);
            AddCommandsToQueue(payload);
        }

        _tcpClient.Close();
        WriteLine("Connection with the master closed");
    }

    private async Task<string> SendAndReceiveCommand(RedisValue message)
    {
        if (!_tcpClient.Connected) throw new ChannelClosedException();

        await _socket.SendAsync(message.Value);
        
        _buffer.Initialize();
        int received = await _socket.ReceiveAsync(_buffer);
        var payload = Encoding.UTF8.GetString(_buffer, 0, received);
        $"Received from master: {payload}".WriteLineEncoded();
        return payload;
    }

    private async Task SendWithConfirmationCommand(RedisValue message, string confirmation = OkString)
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

    private void AddCommandsToQueue(string payload)
    {
        void FilterOrAddCommand(string command)
        {
            if (command[0] == '*') MasterCommandQueue.Add(command);
        }

        var sb = new StringBuilder();

        foreach (var ch in payload)
        {
            if (Delimiters.Contains(ch))
            {
                if (sb.Length > 0)
                {
                    FilterOrAddCommand(sb.ToString());
                    sb.Clear();
                }
            }
            sb.Append(ch);
        }
        
        if (sb.Length > 0) FilterOrAddCommand(sb.ToString());
    }
}