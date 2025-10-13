namespace codecrafters_redis.Commands.Handlers;

internal class Ping : ICommandHandler
{
    public CommandType CommandType => CommandType.Ping;
    public bool SupportsReplication => false;
    public Task<RedisValue> Handle(Command command, ClientConnection connection) => Task.FromResult("PONG".ToSimpleString());
}