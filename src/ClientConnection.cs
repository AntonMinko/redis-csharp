using System.Net.Sockets;

namespace codecrafters_redis;

internal class ClientConnection(int id, Socket socket)
{
    public int Id { get; private set; } = id;
    public Socket Socket { get; private set; } = socket;
    public long LastCommandOffset { get; set; }

    public bool IsReplicaConnection => Id == -1;
}