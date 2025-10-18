using System.Collections.Concurrent;
using System.Net.Sockets;
using codecrafters_redis.Subscriptions;

namespace codecrafters_redis;

internal class ClientConnection(int id, Socket socket)
{
    public int Id { get; private set; } = id;
    public Socket Socket { get; private set; } = socket;
    public long LastCommandOffset { get; set; }

    public bool IsReplicaConnection => Id == -1;
    
    public bool InSubscribedMode { get; private set; }
    
    internal BlockingCollection<PubSubMessage> MessagesQueue { get; } = new();

    private Task? _pubSubBroadcastTask;

    public void EnterSubscribedMode()
    {
        if (InSubscribedMode) return;

        InSubscribedMode = true;
        _pubSubBroadcastTask ??= Task.Run(PubSubBroadcast);
        $"Client {Id}: Entered subscribed mode".WriteLineEncoded();
    }

    private async Task PubSubBroadcast()
    {
        foreach (var message in MessagesQueue.GetConsumingEnumerable())
        {
            try
            {
                $"Sending PubSub message to client {Id}. Channel {message.Channel}, message {message.Message}".WriteLineEncoded();
                var payload = new[] { "message", message.Channel, message.Message }.ToBulkStringArray();
                await Socket.SendAsync(payload.Value);
            }
            catch (Exception e)
            {
                WriteLine($"Unable to send PubSub message to client {Id}. Channel {message.Channel}, message {message.Message}: {e}");
            }
        }
    }
}
