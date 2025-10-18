using System.Diagnostics;
using codecrafters_redis.Commands.Handlers.Validation;
using codecrafters_redis.Storage;
using codecrafters_redis.Subscriptions;
using ValueType = codecrafters_redis.Storage.ValueType;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 1, Max = 2)]
[Supports(StorageType = ValueType.StringArray)]
internal class BLPop(SubscriptionManager subscriptionManager, IStorage storage, Settings settings) : LPopBase(storage, settings)
{
    private const int DelayMs = 100;
    public override CommandType CommandType => CommandType.BLPop;
    public override bool SupportsReplication => false;

    public override bool LongOperation => true;

    protected override async Task<RedisValue> HandleSpecificAsync(Command command, ClientConnection connection)
    {
        string key = command.Arguments[0];
        int timeoutSec = command.Arguments.Length == 2 ? int.Parse(command.Arguments[1]) : 0;
        $"Handling BLPop command. Waiting for key: {key}, timeout: {timeoutSec} sec".WriteLineEncoded();

        if (TryPop(key, 1, out var removedItems)) return new[] {key, removedItems[0]}.ToBulkStringArray();
        
        subscriptionManager.SubscribeFor(EventType.ListPushed, key, connection.Id);

        var stopwatch = Stopwatch.StartNew();
        while (!subscriptionManager.IsEventFired(EventType.ListPushed, key, connection.Id) &&
               !IsTimedOut(timeoutSec, stopwatch))
        {
            await Task.Delay(DelayMs);
        }
        
        var payload = subscriptionManager.UnsubscribeFrom(EventType.ListPushed, key, connection.Id);
        return payload == null ? NullBulkStringArray : new[] {key, payload}.ToBulkStringArray();
    }
    
    private bool IsTimedOut(int timeOutSec, Stopwatch stopwatch) => timeOutSec != 0 && stopwatch.Elapsed.Seconds >= timeOutSec;
}