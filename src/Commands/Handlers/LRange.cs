using codecrafters_redis.Commands.Handlers.Validation;
using codecrafters_redis.Storage;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 3, Max = 3)]
internal class LRange(ListStorage storage, Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.LRange;
    public override bool SupportsReplication => false;

    protected override RedisValue HandleSpecific(Command command, ClientConnection connection)
    {
        var key = command.Arguments[0];
        int start = Convert.ToInt32(command.Arguments[1]);
        int end = Convert.ToInt32(command.Arguments[2]);

        if (!storage.TryGetList(key, out var list)) return EmptyBulkStringArray;

        if (start < 0)
        {
            start = list!.Count + start;
            start = start < 0 ? 0 : start;
        }
        
        end = end < 0 ? list!.Count + end : end;
        end = end >= list!.Count ? list.Count - 1 : end;
        int count = end - start + 1;
        
        if (count <= 0) return EmptyBulkStringArray;
        return list.Skip(start).Take(count).ToBulkStringArray();
    }
}