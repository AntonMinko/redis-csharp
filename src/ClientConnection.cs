using System.Collections.Concurrent;
using System.Net.Sockets;
using codecrafters_redis.Subscriptions;

namespace codecrafters_redis;

internal class ClientConnection(int id, Socket socket)
{
    public int Id { get; } = id;
    public Socket Socket { get; } = socket;
    public long LastCommandOffset { get; set; }

    public bool IsReplicaConnection => Id == -1;
    
    public bool InSubscribedMode { get; private set; }
    
    internal ConcurrentQueue<PubSubMessage> MessagesQueue { get; } = new();

    private Task? _pubSubBroadcastTask;
    private const int PubSubCheckIntervalMs = 100;

    public void EnterSubscribedMode()
    {
        if (InSubscribedMode) return;

        InSubscribedMode = true;
        _pubSubBroadcastTask ??= Task.Run(PubSubBroadcast);
        $"Client {Id}: Entered subscribed mode".WriteLineEncoded();
    }

    private async Task PubSubBroadcast()
    {
        while (true)
        {
            if (!MessagesQueue.TryDequeue(out var message))
            {
                await Task.Delay(PubSubCheckIntervalMs);
                continue;
            }
            
            $"Sending PubSub message to client {Id}. Channel {message.Channel}, message {message.Message}".WriteLineEncoded();
            var payload = new[] { "message", message.Channel, message.Message }.ToBulkStringArray();
            try
            {
                await Socket.SendAsync(payload.Value);
            }
            catch (Exception e)
            {
                WriteLine($"Unable to send PubSub message to client {Id}. Channel {message.Channel}, message {message.Message}: {e}");
            }
        }
    }
}
