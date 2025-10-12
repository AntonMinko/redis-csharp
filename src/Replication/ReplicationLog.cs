namespace codecrafters_redis.Replication;

public class ReplicationLog
{
    record Entry(long Offset, byte[] Payload);
    
    private List<Entry> Entries { get; } = new();
    private Dictionary<long, int> OffsetToIndex { get; } = new();
    private long _nextOffset = 0;
    private readonly object _lockObject = new();

    public long Offset => _nextOffset;
    
    public long Append(byte[] payload)
    {
        lock (_lockObject)
        {
            var offset = _nextOffset;
            Entries.Add(new Entry(offset, payload));
            OffsetToIndex.Add(offset, Entries.Count - 1);
            _nextOffset += payload.Length;
            return _nextOffset;
        }
    }

    public IEnumerable<byte[]> GetCommandsToReplicate(long startOffset)
    {
        if (startOffset >= _nextOffset) yield break;
        
        $"GetCommandsToReplicate: startOffset: {startOffset}, _nextOffset: {_nextOffset}".WriteLineEncoded();

        int startIndex = OffsetToIndex[startOffset];
        var endIndex = Entries.Count - 1;
        for (int i = startIndex; i <= endIndex; i++)
        {
            yield return Entries[i].Payload;
        }
    }
}