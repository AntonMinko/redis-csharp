using System.Net.Sockets;

namespace codecrafters_redis.Replication;

class Replica(Guid id, Socket socket)
{
    public Guid Id { get; private set; } = id;
    public Socket Socket { get; private set; } = socket;
    public long Offset { get; private set; } = 0;

    public async Task SendAsync(byte[] data)
    {
        await Socket.SendAsync(data);
        Offset += data.Length;
    }
}