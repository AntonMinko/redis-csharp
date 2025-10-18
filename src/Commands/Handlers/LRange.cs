using codecrafters_redis.Commands.Handlers.Validation;
using codecrafters_redis.Storage;
using ValueType = codecrafters_redis.Storage.ValueType;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 3, Max = 3)]
[Supports(StorageType = ValueType.StringArray)]
internal class LRange(IStorage storage, Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.LRange;
    public override bool SupportsReplication => false;

    protected override RedisValue HandleSpecific(Command command, ClientConnection connection)
    {
        var key = command.Arguments[0];
        int start = Convert.ToInt32(command.Arguments[1]);
        int end = Convert.ToInt32(command.Arguments[2]);

        var typedValue = storage.Get(key);
        if (typedValue == null) return EmptyBulkStringArray;
        if (!ValidateValueType(typedValue, out var error)) return error!;

        var list = typedValue.Value.GetAsStringList();

        if (start < 0)
        {
            start = list.Count + start;
            start = start < 0 ? 0 : start;
        }
        
        end = end < 0 ? list.Count + end : end;
        end = end >= list.Count ? list.Count - 1 : end;
        int count = end - start + 1;
        
        if (count <= 0) return EmptyBulkStringArray;
        return list.Skip(start).Take(count).ToBulkStringArray();
    }
}