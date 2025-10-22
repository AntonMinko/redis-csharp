using codecrafters_redis.Commands.Handlers.Validation;
using codecrafters_redis.Storage;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 1)]
internal class Type(StorageManager storage, Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.Type;
    public override bool SupportsReplication => false;

    protected override RedisValue HandleSpecific(Command command, ClientConnection connection)
    {
        var key = command.Arguments[0];
        var keyType = storage.GetType(key);

        return keyType.ToString().ToLowerInvariant().ToSimpleString();
    }
}