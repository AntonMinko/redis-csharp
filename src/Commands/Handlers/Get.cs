using codecrafters_redis.Commands.Handlers.Validation;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 1)]
[Supports(StorageType = ValueType.String)]
internal class Get(IStorage storage, Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.Get;
    public override bool SupportsReplication => false;

    protected override async Task<RedisValue> HandleSpecific(Command command, ClientConnection connection)
    {
        var key = command.Arguments[0];
        var typedValue = storage.Get(key);
        if (!ValidateValueType(typedValue, out var error)) return error!;

        return typedValue.ToBulkString();
    }
}