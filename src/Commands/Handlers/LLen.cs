using codecrafters_redis.Commands.Handlers.Validation;
using codecrafters_redis.Storage;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 1, Max = 1)]
internal class LLen(ListStorage storage, Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.LLen;
    public override bool SupportsReplication => false;

    protected override RedisValue HandleSpecific(Command command, ClientConnection connection) =>
        storage.TryGetList(command.Arguments[0], out var list) 
            ? list!.Count.ToIntegerValue() 
            : 0.ToIntegerValue();
}