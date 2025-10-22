using codecrafters_redis.Commands.Handlers.Validation;
using codecrafters_redis.Storage;
using codecrafters_redis.Subscriptions;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 2)]
[ReplicationRole(Role = ReplicationRole.Master)]
internal class RPush(ListStorage storage, Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.RPush;
    public override bool SupportsReplication => true;

    protected override RedisValue HandleSpecific(Command command, ClientConnection connection)
    {
        int count = storage.AddLast(command.Arguments[0], command.Arguments.Skip(1));
        
        return count.ToIntegerValue();
    }
}