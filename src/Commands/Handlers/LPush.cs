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
        var values = command.Arguments.Skip(1).Reverse().ToList();

        var typedValue = storage.Get(key);
        if (typedValue == null)
        {
            storage.Set(key, new(ValueType.StringArray, values));
            return Task.FromResult(values.Count.ToIntegerString());
        }

        if (!ValidateValueType(typedValue, out var error))
        {
            return Task.FromResult(error!);
        }

        var list = typedValue.Value.GetAsStringArray();
        list.InsertRange(0, values);
        
        storage.Set(key, typedValue.Value);
        
        return Task.FromResult(list.Count.ToIntegerString());
    }
}