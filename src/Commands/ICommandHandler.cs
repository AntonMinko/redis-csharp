namespace codecrafters_redis.Commands;

internal interface ICommandHandler
{
    CommandType CommandType { get; }
    bool SupportsReplication { get; }
    bool LongOperation { get; }

    RedisValue Handle(Command command, ClientConnection connection);
    Task<RedisValue> HandleAsync(Command command, ClientConnection connection);
}