using codecrafters_redis.Commands.Handlers.Validation;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 1, Max = 1)]
[Supports(StorageType = ValueType.StringArray)]
internal class LLen(IStorage storage, Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.LLen;
    public override bool SupportsReplication => false;

    protected override Task<RedisValue> HandleSpecific(Command command, ClientConnection connection)
    {
        var key = command.Arguments[0];

        var typedValue = storage.Get(key);
        if (typedValue == null)
        {
            return Task.FromResult(0.ToIntegerString());
        }

        if (!ValidateValueType(typedValue, out var error))
        {
            return Task.FromResult(error!);
        }

        var list = typedValue.Value.GetAsStringList();
        return Task.FromResult(list.Count.ToIntegerString());
    }
}