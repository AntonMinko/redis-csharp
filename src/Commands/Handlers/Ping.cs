namespace codecrafters_redis.Commands.Handlers;

internal class Ping : ICommandHandler
{
    public CommandType CommandType => CommandType.Ping;
    public bool SupportsReplication => false;
    public bool LongOperation => false;

    RedisValue ICommandHandler.Handle(Command command, ClientConnection connection) => "PONG".ToSimpleString();

    public Task<RedisValue> HandleAsync(Command command, ClientConnection connection) => throw new NotImplementedException();
}