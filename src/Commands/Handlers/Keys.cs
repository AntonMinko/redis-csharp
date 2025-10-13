using codecrafters_redis.Commands.Handlers.Validation;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 1, Max = 2)]
internal class Keys(IStorage storage, Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.Keys;
    public override bool SupportsReplication => false;

    protected override async Task<RedisValue> HandleSpecific(Command command, ClientConnection connection)
    {
        string pattern = command.Arguments[0].ToUpperInvariant();

        if (pattern != "*")
        {
            $"Unsupported keys pattern: {pattern}".WriteLineEncoded();
        }
        
        return storage.GetAllKeys().ToArray().ToBulkStringArray();
    }
}