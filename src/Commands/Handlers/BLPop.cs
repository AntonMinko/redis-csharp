using System.Collections.Concurrent;
using System.Diagnostics;
using codecrafters_redis.Commands.Handlers.Validation;
using codecrafters_redis.Storage;
using codecrafters_redis.Subscriptions;
using ValueType = codecrafters_redis.Storage.ValueType;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 1, Max = 2)]
internal class BLPop(PubSub pubSub, ListStorage storage, Settings settings) : LPopBase(storage, settings)
{
    private const int DelayMs = 100;
    public override CommandType CommandType => CommandType.BLPop;
    public override bool SupportsReplication => false;

    public override bool LongOperation => true;

    protected override async Task<RedisValue> HandleSpecificAsync(Command command, ClientConnection connection)
    {
        string key = command.Arguments[0];
        double timeoutSec = command.Arguments.Length == 2 ? double.Parse(command.Arguments[1]) : 0;
        int timeoutMs = (int) (timeoutSec * 1000);
        $"Handling BLPop command. Waiting for key: {key}, timeout: {timeoutSec} sec".WriteLineEncoded();

        if (TryPop(key, 1, out var removedItems)) return new[] {key, removedItems[0]}.ToBulkStringArray();

        var messageQueue = new ConcurrentQueue<PubSubMessage>();
        pubSub.Subscribe(EventType.ListPushed, key, connection.Id, messageQueue);

        var stopwatch = Stopwatch.StartNew();
        while (!messageQueue.Any() && !IsTimedOut(timeoutMs, stopwatch))
        {
            await Task.Delay(DelayMs);
        }
        
        pubSub.Unsubscribe(EventType.ListPushed, key, connection.Id);
        return messageQueue.TryDequeue(out var message)
            ? new[] { key, message.Message }.ToBulkStringArray()
            : NullBulkStringArray;
    }
    
    private bool IsTimedOut(int timeoutMs, Stopwatch stopwatch) => timeoutMs != 0 && stopwatch.Elapsed.TotalMilliseconds >= timeoutMs;
}