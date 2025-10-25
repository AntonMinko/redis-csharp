using codecrafters_redis.Commands.Handlers.Validation;
using codecrafters_redis.Storage;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 4)]
[ReplicationRole(Role = ReplicationRole.Master)]
internal class XAdd(StreamStorage storage, Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.XAdd;
    public override bool SupportsReplication => true;

    protected override RedisValue HandleSpecific(Command command, ClientConnection connection)
    {
        if (command.Arguments.Length % 2 != 0) return "wrong number of arguments for 'xadd' command".ToErrorString();
        
        string streamKey = command.Arguments[0];
        string entryKey = command.Arguments[1];
        
        var entries = command.Arguments
            .Skip(2)
            .Chunk(2)
            .ToDictionary(entry => entry[0], entry => entry[1]);
        
        var entryId = storage.Append(streamKey, entryKey, entries);
        return entryId.ToBulkString();
    }
}