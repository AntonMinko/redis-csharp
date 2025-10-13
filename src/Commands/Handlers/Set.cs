using codecrafters_redis.Commands.Handlers.Validation;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 2)]
[ReplicationRole(Role = ReplicationRole.Master)]
internal class Set(IStorage storage, Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.Set;
    public override bool SupportsReplication => true;

    protected override Task<RedisValue> HandleSpecific(Command command, ClientConnection connection)
    {
        var key = command.Arguments[0];
        var value = command.Arguments[1];
        int? expiresAfterMs = null;
        if (command.Arguments.Length == 4 && command.Arguments[2].ToUpperInvariant() == "PX")
        {
            if (int.TryParse(command.Arguments[3], out int ms))
            {
                expiresAfterMs = ms;
            }
        }
        
        storage.Set(key, new(ValueType.String, value), expiresAfterMs);
        return Task.FromResult(OkBytes);
    }
}