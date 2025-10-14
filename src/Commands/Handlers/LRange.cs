using codecrafters_redis.Commands.Handlers.Validation;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 3, Max = 3)]
[Supports(StorageType = ValueType.StringArray)]
internal class LRange(IStorage storage, Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.LRange;
    public override bool SupportsReplication => false;

    protected override Task<RedisValue> HandleSpecific(Command command, ClientConnection connection)
    {
        var key = command.Arguments[0];
        int start = Convert.ToInt32(command.Arguments[1]);
        int end = Convert.ToInt32(command.Arguments[2]);

        var typedValue = storage.Get(key);
        if (typedValue == null) return Task.FromResult(EmptyBulkStringArray);
        if (!ValidateValueType(typedValue, out var error)) return Task.FromResult(error!);

        var list = typedValue.Value.GetAsStringList();

        if (start < 0)
        {
            start = list.Count + start;
            start = start < 0 ? 0 : start;
        }
        
        end = end < 0 ? list.Count + end : end;
        end = end >= list.Count ? list.Count - 1 : end;
        int count = end - start + 1;
        
        if (count <= 0) return Task.FromResult(EmptyBulkStringArray);
        return Task.FromResult(list.Skip(start).Take(count).ToBulkStringArray());
    }
}