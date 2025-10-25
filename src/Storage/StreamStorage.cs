using System.Collections.Concurrent;

namespace codecrafters_redis.Storage;
using StreamEntry = LinkedList<Dictionary<string, string>>;

internal class StreamStorage
{
    class Stream
    {
        private SortedSet<long> _keys = new();
        private Dictionary<long, StreamEntry> _entries = new();
        private long _lastKey = long.MinValue;

        public void Append(string entryKey, Dictionary<string, string> values)
        {
            var (key, index) = ToKeyAndIndex(entryKey);
            
            if (!_entries.ContainsKey(key)) _entries.Add(key, new());
            _entries[key].AddLast(values);
        }

        private (long key, int index) ToKeyAndIndex(string entryKey)
        {
            var parts = entryKey.Split('-');
            var key = long.Parse(parts[0]);
            var index = int.Parse(parts[1]);
            return (key, index);
        }
    }

    private readonly ConcurrentDictionary<string, Stream> _store = new();

    public string Append(string streamKey, string entryKey, Dictionary<string, string> values)
    {
        if (!_store.ContainsKey(streamKey)) _store.TryAdd(streamKey, new Stream());
        
        _store[streamKey].Append(entryKey, values);
        
        return entryKey;
    }
    
    public bool HasKey(string streamKey) => _store.ContainsKey(streamKey);
}