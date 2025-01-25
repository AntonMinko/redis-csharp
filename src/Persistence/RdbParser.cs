using static System.Text.Encoding;
using static System.Console;

namespace codecrafters_redis.Persistence;

internal class RdbParser
{
    internal async Task<DataModel> ParseAsync(string backupFile)
    {
        var dataModel = new DataModel();
        var buffer = new byte[1024];
        
        using var fileStream = new FileStream(backupFile, FileMode.Open);
        //using var reader = new StreamReader(fileStream);
        
        await ParseHeaderAsync(fileStream, buffer, dataModel);
        await ParseMetadataAsync(fileStream, buffer, dataModel);
        await ParseDatabasesAsync(fileStream, buffer, dataModel);
        
        return dataModel;
    }

    private async Task ParseHeaderAsync(Stream stream, byte[] buffer, DataModel dataModel)
    {
        int count = await stream.ReadAsync(buffer, 0, 9);
        if (count != 9)
        {
            throw new InvalidDataException("Unexpected end of file. Expected 9 bytes in the header.");
        }
        
        string redisStr = UTF8.GetString(buffer, 0, 5);
        if (redisStr != "REDIS")
        {
            throw new InvalidDataException($"The file must start with the magic string REDIS, but was {redisStr}");
        }
        
        string versionStr = UTF8.GetString(buffer, 5, 4);
        if (!int.TryParse(versionStr, out int version))
        {
            throw new InvalidDataException($"Failed to parse RDB version {versionStr}");
        }
        
        dataModel.RdbVersion = version;
    }

    private async Task ParseMetadataAsync(Stream stream, byte[] buffer, DataModel dataModel)
    {
        while (true)
        {
            var auxByte = (byte)stream.ReadByte();
            if (auxByte != 0xFA)
            {
                stream.Position -= 1;
                break;
            }
            
            string name = await ReadStringAsync(stream, buffer);
            string value = await ReadStringAsync(stream, buffer);
            dataModel.Metadata.Add(name, value);
        }
    }
    
    private async Task ParseDatabasesAsync(Stream stream, byte[] buffer, DataModel dataModel)
    {
        while (true)
        {
            var auxByte = (byte)stream.ReadByte();
            if (auxByte != 0xFE)
            {
                stream.Position -= 1;
                break;
            }
            
            var parsedDbNumber = await GetLengthEncodedIntAsync(stream, buffer);
            var database = await ParseDatabaseAsync(stream, buffer);
            dataModel.Databases.Add(parsedDbNumber.Value, database);
        }
    }

    private async Task<IDictionary<string, StorageValue>> ParseDatabaseAsync(Stream stream, byte[] buffer)
    {
        var hashTableMarker = (byte)stream.ReadByte();
        if (hashTableMarker != 0xFB)
        {
            throw new InvalidDataException($"Unexpected content. Expected 0xFB byte indicating the start of hash table, but was {hashTableMarker}");
        }

        var kvp = new Dictionary<string, StorageValue>();
        
        var parsedSimpleKeys = await GetLengthEncodedIntAsync(stream, buffer);
        var parsedKeysWithExpiration = await GetLengthEncodedIntAsync(stream, buffer);

        for (int i = 0; i < parsedSimpleKeys.Value; i++)
        {
            var valueTypeByte = (byte)stream.ReadByte();
            string key = await ReadStringAsync(stream, buffer);

            switch (valueTypeByte)
            {
                case 0x00:
                    string value = await ReadStringAsync(stream, buffer);
                    kvp.Add(key, new StorageValue(value));
                    break;
                case 0xFC:
                    //TODO
                default:
                    throw new NotImplementedException($"Unsupported content type {valueTypeByte}");
            }
            
        }

        return kvp;
    }

    private async Task<string> ReadStringAsync(Stream stream, byte[] buffer)
    {
        var result = await GetLengthEncodedIntAsync(stream, buffer);
        if (!result.IsStringLength)
        {
            return result.Value.ToString();
        }
        
        string str = "";
        int strLen = result.Value;
        while (strLen > 0)
        {
            int len = await stream.ReadAsync(buffer, 0, Math.Min(buffer.Length, strLen));
            str += UTF8.GetString(buffer, 0, len);
            strLen -= len;
        }

        return str;
    }
    
    private async Task<IntParseResult> GetLengthEncodedIntAsync(Stream stream, byte[] buffer)
    {
        buffer[0] = (byte)stream.ReadByte();
        var firstTwoBits = (buffer[0] & 0b1100_0000) >> 6;
        int result = 0;
        switch (firstTwoBits)
        {
            case 0b00:
                result = buffer[0];
                return new IntParseResult(result, true);
            case 0b01:
                buffer[1] = (byte)stream.ReadByte();
                int firstByte = (buffer[0] & 0b0011_1111) << 8;
                result = firstByte | buffer[1];
                return new IntParseResult(result, true);
            case 0b10:
                await stream.ReadExactlyAsync(buffer, 0, 4);
                result = BitConverter.ToInt32(buffer, 0);
                return new IntParseResult(result, true);
            case 0b11:
            default:
                // special case, the encoded item is an integer, not a string with length prefix
                stream.Position -= 1;
                result = await ParseIntAsync(stream, buffer);
                return new IntParseResult(result, false);
        }
    }
    
    private async Task<int> ParseIntAsync(Stream stream, byte[] buffer)
    {
        var encodingByte = stream.ReadByte();
        switch (encodingByte)
        {
            case 0xC0:
                return stream.ReadByte();
            case 0xC1:
                await stream.ReadExactlyAsync(buffer, 0, 2);
                // convert from little-endian
                return buffer[1] << 8 | buffer[0];
            case 0xC2:
                await stream.ReadExactlyAsync(buffer, 0, 4);
                // convert from little-endian
                return buffer[3] << 24 | buffer[2] << 16 | buffer[1] << 8 | buffer[0];
            default:
                throw new InvalidDataException("Stream does not contain integer value");
        }
    }

}

internal record struct IntParseResult(int Value, bool IsStringLength);

internal class DataModel
{
    public int RdbVersion { get; set; }
    public Dictionary<string, string> Metadata { get; } = new();
    
    public Dictionary<int, IDictionary<string, StorageValue>> Databases { get; } = new();
}