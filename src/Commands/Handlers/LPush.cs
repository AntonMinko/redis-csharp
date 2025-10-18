using codecrafters_redis.Commands.Handlers.Validation;
using codecrafters_redis.Storage;
using codecrafters_redis.Subscriptions;
using ValueType = codecrafters_redis.Storage.ValueType;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 2)]
[Supports(StorageType = ValueType.StringArray)]
[ReplicationRole(Role = ReplicationRole.Master)]
internal class LPush(SubscriptionManager subscriptionManager, IStorage storage, Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.LPush;
    public override bool SupportsReplication => true;

    protected override RedisValue HandleSpecific(Command command, ClientConnection connection)
    {
        var key = command.Arguments[0];
        
        var storedValue = storage.Get(key);
        if (!ValidateValueType(storedValue, out var error)) return error!;
        
        LinkedList<string> values = storedValue != null
            ? storedValue.Value.GetAsStringList()
            : new LinkedList<string>();

        for (int i = 1; i < command.Arguments.Length; i++)
        {
            var value = command.Arguments[i];
            if (subscriptionManager.FireEvent(EventType.ListPushed, key, value)) continue;
            
            values.AddFirst(value);
        }
        
        storage.Set(key, new TypedValue(ValueType.StringArray, values));
        
        return values.Count.ToIntegerString();
    }
}