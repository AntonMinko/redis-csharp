using System.Collections.Concurrent;
using codecrafters_redis.Subscriptions;

namespace codecrafters_redis.Storage;

internal class ListStorage(PubSub pubSub)
{
    private readonly ConcurrentDictionary<string, LinkedList<string>> _store = new();

    public bool TryGetList(string key, out IReadOnlyCollection<string>? list)
    {
        list = null;
        if (_store.TryGetValue(key, out var linkedList))
        {
            list = linkedList;
            return true;
        }
        return false;
    }

    //public void Set(string key, LinkedList<string> value) => _store[key] = value;

    public int AddFirst(string key, IEnumerable<string> values)
    {
        var list = GetOrAdd(key);
        int count = list.Count;

        foreach (var value in values)
        {
            int deliveries = pubSub.Publish(EventType.ListPushed, key, value);
            if (deliveries == 0)
            {
                list.AddFirst(value);
            }
            count++;
        }
        
        return count;
    }

    public int AddLast(string key, IEnumerable<string> values)
    {
        var list = GetOrAdd(key);
        int count = list.Count;

        foreach (var value in values)
        {
            int deliveries = pubSub.Publish(EventType.ListPushed, key, value);
            if (deliveries == 0)
            {
                list.AddLast(value);
            }
            count++;
        }
        
        return count;
    }

    public bool TryRemoveFirst(string key, out string? value)
    {
        value = null;
        if (!_store.TryGetValue(key, out var list)) return false;
        
        value = list.First!.Value;
        
        list.RemoveFirst();
        if (list.Count == 0) _store.TryRemove(key, out _);
        
        return true;
    }
    
    public IEnumerable<string> Keys => _store.Keys;
    
    private LinkedList<string> GetOrAdd(string key) => _store.GetOrAdd(key, _ => new LinkedList<string>());
}