using codecrafters_redis.Storage;

namespace codecrafters_redis.Commands.Handlers;

internal abstract class LPopBase(IStorage storage, Settings settings) : BaseHandler(settings)
{
    protected bool TryPop(string key, int count, out List<string> removedItems)
    {
        removedItems = [];
        var typedValue = storage.Get(key);
        if (typedValue == null) return false;
        if (!ValidateValueType(typedValue, out var error)) return false;
        
        var list = typedValue.Value.GetAsStringList();
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

        return true;
    }
}