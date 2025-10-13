namespace codecrafters_redis.Commands;

internal interface ICommandHandler
{
    CommandType CommandType { get; }
    bool SupportsReplication { get; }

    Task<RedisValue> Handle(Command command, ClientConnection connection);
}