using codecrafters_redis.Commands.Handlers.Validation;
using codecrafters_redis.Storage;
using ValueType = codecrafters_redis.Storage.ValueType;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 1, Max = 1)]
[Supports(StorageType = ValueType.StringArray)]
internal class LLen(IStorage storage, Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.LLen;
    public override bool SupportsReplication => false;

    protected override RedisValue HandleSpecific(Command command, ClientConnection connection)
    {
        var key = command.Arguments[0];

        var typedValue = storage.Get(key);
        if (typedValue == null)
        {
            return 0.ToIntegerValue();
        }

        if (!ValidateValueType(typedValue, out var error)) return error!;

        var list = typedValue.Value.GetAsStringList();
        return list.Count.ToIntegerValue();
    }
}