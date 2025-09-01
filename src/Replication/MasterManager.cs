using System.Net.Sockets;
using codecrafters_redis.Helpers;

namespace codecrafters_redis.Replication;

public class MasterManager
{
    private const string EmptyRdbFile = "UkVESVMwMDEx+glyZWRpcy12ZXIFNy4yLjD6CnJlZGlzLWJpdHPAQPoFY3RpbWXCbQi8ZfoIdXNlZC1tZW3CsMQQAPoIYW9mLWJhc2XAAP/wbjv+wP9aog==";

    private Socket? _replicaSocket;
    
    public void InitReplicaConnection(Socket socket, byte[] pSyncResponse)
    {
        _replicaSocket = socket;
        
        byte[] emptyRdbFileBytes = Convert.FromBase64String(EmptyRdbFile).ToBinaryContent();
        _replicaSocket.Send(pSyncResponse);
        _replicaSocket.Send(emptyRdbFileBytes);
    }

    public async Task PropagateCommand(string[] command)
    {
        if (_replicaSocket == null || _replicaSocket.Connected == false) return;
        
        switch (command[0].ToUpperInvariant())
        {
            case "SET":
                await _replicaSocket.SendAsync(command.ToBulkStringArray());
                break;
            default:
                return;
        }
    }
}