using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Helpers;
using codecrafters_redis.Replication;
using codecrafters_redis.UserSettings;
using static System.Console;
using static codecrafters_redis.Helpers.RedisTypes;

namespace codecrafters_redis;

internal class CommandHandler(IStorage storage, Settings settings, MasterManager masterManager)
{
    public byte[] Handle(string[] command, Socket socket)
    {
        switch (command[0].ToUpperInvariant())
        {
            case "PING":
                return "PONG".ToSimpleString();
            case "ECHO":
                return command[1].ToSimpleString();
            case "GET":
                return HandleGet(command);
            case "SET":
                return HandleSet(command);
            case "CONFIG":
                return HandleConfig(command);
            case "KEYS":
                return HandleKeys(command);
            case "INFO":
                return HandleInfo(command);
            case "REPLCONF":
                return HandlePerfConf(command);
            case "PSYNC":
                return HandlePSync(command, socket);
            default:
                WriteLine("Unknown command: " + String.Join(" ", command));
                return $"Unknown command {command[0]}".ToErrorString();
        }
    }

    private byte[] HandlePSync(string[] command, Socket socket)
    {
        if (settings.Replication.Role != ReplicationRole.Master) return "ERR Only master can handle PSYNC commands".ToErrorString();
        Console.WriteLine("Handle PSync");
        
        var fullResync = $"FULLRESYNC {settings.Replication.MasterReplicaSettings!.MasterReplId} {settings.Replication.MasterReplicaSettings.MasterReplOffset}".ToSimpleString();
        
        masterManager.InitReplicaConnection(socket, fullResync);
        return [];
    }

    private byte[] HandlePerfConf(string[] command)
    {
        return OkBytes;
    }

    private byte[] HandleInfo(string[] command)
    {
        if (command.Length > 3)
        {
            return "ERR wrong number of arguments for 'info' command".ToErrorString();
        }
        
        string section = command.Length >= 2 ? command[1].ToUpperInvariant() : "";

        if (section != "" && section != "REPLICATION")
        {
            WriteLine($"Unsupported section: {section}");
            return $"Unknown info command section {section}".ToErrorString();
        }

        var sb = new StringBuilder();
        sb.AppendLine("# Replication");
        sb.AppendLine($"role:{settings.Replication.Role.ToString().ToLowerInvariant()}");
        if (settings.Replication.SlaveReplicaSettings != null)
        {
            sb.AppendLine($"master_host:{settings.Replication.SlaveReplicaSettings.MasterHost}");
            sb.AppendLine($"master_port:{settings.Replication.SlaveReplicaSettings.MasterPort}");
        }
        if (settings.Replication.MasterReplicaSettings != null)
        {
            sb.AppendLine($"master_replid:{settings.Replication.MasterReplicaSettings.MasterReplId}");
            sb.AppendLine($"master_repl_offset:{settings.Replication.MasterReplicaSettings.MasterReplOffset}");
        }

        return sb.ToString().ToBulkString();
    }

    private byte[] HandleKeys(string[] command)
    {
        if (command.Length == 1 || command.Length > 3)
        {
            return "ERR wrong number of arguments for 'keys' command".ToErrorString();
        }
        
        string pattern = command[1].ToUpperInvariant();

        if (pattern != "*")
        {
            WriteLine($"Unsupported keys pattern: {pattern}");
        }
        
        return storage.GetAllKeys().ToArray().ToBulkStringArray();
    }

    private byte[] HandleConfig(string[] command)
    {
        if (command.Length == 1 || command.Length > 3)
        {
            return "ERR wrong number of arguments for 'config' command".ToErrorString();
        }

        if (command[1].ToUpperInvariant() != "GET")
        {
            return $"ERR unknown subcommand '{command[1]}'".ToErrorString();
        }
        
        if (command.Length == 2)
        {
            return "ERR wrong number of arguments for 'config|get' command".ToErrorString();
        }
        
        switch (command[2].ToUpperInvariant())
        {
            case "DIR":
                return new[] {"dir", settings.Persistence.Dir}.ToBulkStringArray();
            case "DBFILENAME":
                return new[] {"dbfilename", settings.Persistence.DbFileName}.ToBulkStringArray();
            default:
                return NullBulkStringArray;
        }
    }

    private byte[] HandleSet(string[] command)
    {
        if (command.Length < 3) return "ERR wrong number of arguments for 'set' command".ToErrorString();
        
        var key = command[1];
        var value = command[2];
        int? expiresAfterMs = null;
        if (command.Length == 5 && command[3].ToUpperInvariant() == "PX")
        {
            if (int.TryParse(command[4], out int ms))
            {
                expiresAfterMs = ms;
            }
        }
        
        storage.Set(key, value, expiresAfterMs);
        return OkBytes;
    }

    private byte[] HandleGet(string[] command)
    {
        if (command.Length < 2) return NullBulkString;
        
        var key = command[1];
        return storage.Get(key).ToBulkString();
    }
}