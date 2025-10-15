using codecrafters_redis.Commands.Handlers.Validation;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Min = 1, Max = 2)]
[Supports(StorageType = ValueType.StringArray)]
internal class LPop(IStorage storage, Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.LPop;
    public override bool SupportsReplication => false;

    protected override Task<RedisValue> HandleSpecific(Command command, ClientConnection connection)
    {
        var key = command.Arguments[0];
        bool hasCountArg = command.Arguments.Length == 2;
        var count = hasCountArg ? int.Parse(command.Arguments[1]) : 1;

        var typedValue = storage.Get(key);
        if (typedValue == null) return Task.FromResult(EmptyBulkStringArray);
        if (!ValidateValueType(typedValue, out var error)) return Task.FromResult(error!);

        var list = typedValue.Value.GetAsStringList();
        var removedItems = new List<string>();
        while (count > 0 && list.Count > 0)
        {
            removedItems.Add(list.First());
            list.RemoveFirst();
            count--;
        }
        
        if (list.Count == 0)
        {
            storage.Remove(key);
        }
        else
        {
            storage.Set(key, typedValue.Value);
        }

        return hasCountArg
            ? Task.FromResult(removedItems.ToBulkStringArray())
            : Task.FromResult(removedItems[0].ToBulkString());
    }
}