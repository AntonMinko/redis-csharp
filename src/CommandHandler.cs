using System.Text;

namespace codecrafters_redis;

public class CommandHandler(IStorage storage)
{
    public byte[] Handle(List<string> command)
    {
        switch (command[0].ToUpperInvariant())
        {
            case "PING":
                return SimpleString("PONG");
            case "ECHO":
                return SimpleString(command[1]);
            case "GET":
                var val = HandleGet(command);
                return BulkString(val);
            case "SET":
                var errorMessage = HandleSet(command);
                return errorMessage != null ? ErrorString(errorMessage) : OkString();
            default:
                Console.WriteLine("Unknown command: " + String.Join(" ", command));
                return ErrorString($"Unknown command {command[0]}");
        }
    }

    private string? HandleSet(List<string> command)
    {
        if (command.Count < 3) return "ERR wrong number of arguments for 'set' command";
        
        var key = command[1];
        var value = command[2];
        int? expiresAfterMs = null;
        if (command.Count == 5 && command[3].ToUpperInvariant() == "PX")
        {
            if (int.TryParse(command[4], out int ms))
            {
                expiresAfterMs = ms;
            }
        }
        
        storage.Set(key, value, expiresAfterMs);
        return null;
    }

    private string? HandleGet(List<string> command)
    {
        if (command.Count < 2) return null;
        
        var key = command[1];
        return storage.Get(key);
    }

    private byte[] SimpleString(string s)
    {
        return Encoding.UTF8.GetBytes($"+{s}\r\n");
    }

    private byte[] ErrorString(string message)
    {
        return Encoding.UTF8.GetBytes($"-{message}\r\n");
    }

    private byte[] BulkString(string? s)
    {
        if (s == null)
        {
            return Encoding.UTF8.GetBytes("$-1\r\n");
        }
        return Encoding.UTF8.GetBytes($"${s.Length}\r\n{s}\r\n");
    }

    private byte[] OkString()
    {
        return Encoding.UTF8.GetBytes("+OK\r\n");
    }
}