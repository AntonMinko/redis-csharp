using codecrafters_redis.Commands.Handlers.Validation;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 3, Max = 3)]
[Supports(StorageType = ValueType.StringArray)]
internal class LRange(IStorage storage, Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.LRange;
    public override bool SupportsReplication => false;

    protected override async Task<RedisValue> HandleSpecific(Command command, ClientConnection connection)
    {
        var key = command.Arguments[0];
        int start = Convert.ToInt32(command.Arguments[1]);
        int end = Convert.ToInt32(command.Arguments[2]);

        var typedValue = storage.Get(key);
        if (typedValue == null) return EmptyBulkStringArray;
        
        if (!ValidateValueType(typedValue, out var error))
        {
            return error!;
        }

        var list = typedValue.Value.GetAsStringArray();
        start = start < 0 ? 0 : start;
        end = end >= list.Count ? list.Count - 1 : end;
        if (start > end) return EmptyBulkStringArray;
        return list.Skip(start).Take(end - start + 1).ToBulkStringArray();
    }
}