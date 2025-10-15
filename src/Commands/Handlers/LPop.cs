using codecrafters_redis.Commands.Handlers.Validation;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 1, Max = 1)]
[Supports(StorageType = ValueType.StringArray)]
internal class LPop(IStorage storage, Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.LPop;
    public override bool SupportsReplication => false;

    protected override Task<RedisValue> HandleSpecific(Command command, ClientConnection connection)
    {
        var key = command.Arguments[0];

        var typedValue = storage.Get(key);
        if (typedValue == null) return Task.FromResult(EmptyBulkStringArray);
        if (!ValidateValueType(typedValue, out var error)) return Task.FromResult(error!);

        var list = typedValue.Value.GetAsStringList();
        var element = list.First();
        list.RemoveFirst();
        if (list.Count == 0)
        {
            storage.Remove(key);
        }
        else
        {
            storage.Set(key, typedValue.Value);
        }
        
        return Task.FromResult(element.ToBulkString());
    }
}