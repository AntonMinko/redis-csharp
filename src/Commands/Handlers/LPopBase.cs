using codecrafters_redis.Storage;

namespace codecrafters_redis.Commands.Handlers;

internal abstract class LPopBase(ListStorage storage, Settings settings) : BaseHandler(settings)
{
    protected bool TryPop(string key, int count, out List<string> removedValues)
    {
        removedValues = [];

        while (count > 0 && storage.TryRemoveFirst(key, out var value))
        {
            removedValues.Add(value!);
            count--;
        }

        return removedValues.Count > 0;
    }
}