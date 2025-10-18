using codecrafters_redis.Commands.Handlers.Validation;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 1)]
internal class Echo(Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.Echo;
    public override bool SupportsReplication => false;
    protected override RedisValue HandleSpecific(Command command, ClientConnection connection) =>
        command.Arguments[0].ToSimpleString();
}