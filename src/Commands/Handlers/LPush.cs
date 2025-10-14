using codecrafters_redis.Commands.Handlers.Validation;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 2)]
[Supports(StorageType = ValueType.StringArray)]
[ReplicationRole(Role = ReplicationRole.Master)]
internal class LPush(IStorage storage, Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.LPush;
    public override bool SupportsReplication => true;

    protected override Task<RedisValue> HandleSpecific(Command command, ClientConnection connection)
    {
        var key = command.Arguments[0];
        
        var storedValue = storage.Get(key);
        if (!ValidateValueType(storedValue, out var error))
        {
            return Task.FromResult(error!);
        }
        
        LinkedList<string> values = storedValue != null
            ? storedValue.Value.GetAsStringList()
            : new LinkedList<string>();

        for (int i = 1; i < command.Arguments.Length; i++)
        {
            values.AddFirst(command.Arguments[i]);
        }
        
        storage.Set(key, new TypedValue(ValueType.StringArray, values));
        
        return Task.FromResult(values.Count.ToIntegerString());
    }
}