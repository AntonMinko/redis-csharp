using System.Text;
using codecrafters_redis.Commands.Handlers.Validation;

namespace codecrafters_redis.Commands.Handlers;

[Arguments(Max = 2)]
internal class Info(Settings settings) : BaseHandler(settings)
{
    public override CommandType CommandType => CommandType.Info;
    public override bool SupportsReplication => false;

    protected override RedisValue HandleSpecific(Command command, ClientConnection connection)
    {
        string section = command.Arguments.Length >= 1 ? command.Arguments[0].ToUpperInvariant() : "";

        if (section != "" && section != "REPLICATION")
        {
            return $"Unknown info command section {section}".ToErrorString();
        }

        var sb = new StringBuilder();
        sb.AppendLine("# Replication");
        sb.AppendLine($"role:{Settings.Replication.Role.ToString().ToLowerInvariant()}");
        if (Settings.Replication.SlaveReplicaSettings != null)
        {
            sb.AppendLine($"master_host:{Settings.Replication.SlaveReplicaSettings.MasterHost}");
            sb.AppendLine($"master_port:{Settings.Replication.SlaveReplicaSettings.MasterPort}");
        }
        if (Settings.Replication.MasterReplicaSettings != null)
        {
            sb.AppendLine($"master_replid:{Settings.Replication.MasterReplicaSettings.MasterReplId}");
            sb.AppendLine($"master_repl_offset:{Settings.Replication.MasterReplicaSettings.MasterReplOffset}");
        }

        return sb.ToString().ToBulkString();
    }
}