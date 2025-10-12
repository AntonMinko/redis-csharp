using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Replication;

class Replica(int id, Socket socket)
{
    public int Id { get; private set; } = id;
    public Socket Socket { get; private set; } = socket;
    public long Offset { get; private set; } = 0;
    public long AckOffset { get; set; } = 0;

    public async Task SendAsync(byte[] data)
    {
        await Socket.SendAsync(data);
        Offset += data.Length;
    }

    public async Task GetAckOffset(long replicationLogOffset)
    {
        if (!Socket.Connected)
        {
            $"Socket for replica {Id} is not connected.".WriteLineEncoded();
            return;
        }
        
        while (Offset < replicationLogOffset)
        {
            $"GetAckOffset for replica {Id}, waiting for replication: replica sent  offset {Offset}, log offset {replicationLogOffset}".WriteLineEncoded();
            await Task.Delay(5);
        }
        
        $"Going to send REPLCONF to replica {Id}".WriteLineEncoded();
        var command = new[] { "REPLCONF", "GETACK", "*" }.ToBulkStringArray();
        await Socket.SendAsync(command.Value);
    }
}