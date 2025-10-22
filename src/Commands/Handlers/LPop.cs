using codecrafters_redis.Commands.Handlers.Validation;
using codecrafters_redis.Storage;
using ValueType = codecrafters_redis.Storage.ValueType;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 1, Max = 2)]
internal class LPop(ListStorage storage, Settings settings) : LPopBase(storage, settings)
{
    public override CommandType CommandType => CommandType.LPop;
    public override bool SupportsReplication => false;

    protected override RedisValue HandleSpecific(Command command, ClientConnection connection)
    {
        var key = command.Arguments[0];
        bool hasCountArg = command.Arguments.Length == 2;
        var count = hasCountArg ? int.Parse(command.Arguments[1]) : 1;

        if (!TryPop(key, count, out var removedItems)) return EmptyBulkStringArray;
        return hasCountArg
            ? removedItems.ToBulkStringArray()
            : removedItems[0].ToBulkString();
    }
}