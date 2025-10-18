using codecrafters_redis.Commands.Handlers.Validation;
using codecrafters_redis.Storage;
using ValueType = codecrafters_redis.Storage.ValueType;

namespace codecrafters_redis.Commands;

internal abstract class BaseHandler(Settings settings) : ICommandHandler
{
    public abstract CommandType CommandType { get; }
    public abstract bool SupportsReplication { get; }
    public virtual bool LongOperation => false;
    protected Settings Settings { get; } = settings;
    protected virtual Task<RedisValue> HandleSpecificAsync(Command command, ClientConnection connection) => throw new NotImplementedException();
    protected virtual RedisValue HandleSpecific(Command command, ClientConnection connection) => throw new NotImplementedException();

    public RedisValue Handle(Command command, ClientConnection connection)
    {
        if (!ValidateRole(command, connection, out var error) ||
            !ValidateArguments(command, out error))
        {
            return error!;
        }
        
        return HandleSpecific(command, connection);
    }

    public async Task<RedisValue> HandleAsync(Command command, ClientConnection connection)
    {
        if (!ValidateRole(command, connection, out var error) ||
            !ValidateSubscribedMode(command, connection, out error) ||
            !ValidateArguments(command, out error))
        {
            return error!;
        }
        
        return await HandleSpecificAsync(command, connection);
    }

    private bool ValidateArguments(Command command, out RedisValue? error)
    {
        error = null;
        var arguments = GetAttribute<ArgumentsAttribute>();
        if (arguments is null) return true;

        if (arguments.Min <= command.Arguments.Length &&
            command.Arguments.Length <= arguments.Max)
        {
            return true;
        }
        
        error = $"ERR wrong number of arguments for command {command.Type}".ToErrorString();
        return false;
    }
    
    private bool ValidateRole(Command command, ClientConnection connection, out RedisValue? error)
    {
        error = null;
        var roleAttribute = GetAttribute<ReplicationRoleAttribute>();
        if (roleAttribute is null) return true;

        if (SupportsReplication && connection.IsReplicaConnection ||
            Settings.Replication.Role == roleAttribute.Role)
        {
            return true;
        }

        error = $"Only {roleAttribute.Role} can handle {command.Type} command".ToErrorString();
        return false;
    }
    
    private bool ValidateSubscribedMode(Command command, ClientConnection connection, out RedisValue? error)
    {
        error = $"Can't execute '{command.Type.ToString().ToLowerInvariant()}': only (P|S)SUBSCRIBE / (P|S)UNSUBSCRIBE / PING / QUIT / RESET are allowed in this context".ToErrorString();
        var attribute = GetAttribute<SupportedInSubscribedModeAttribute>();
        if (attribute is null) return connection.InSubscribedMode == false;

        if (connection.InSubscribedMode && !attribute.IsSupported) return false;

        return true;
    }

    private T? GetAttribute<T>() where T : Attribute => (T?) Attribute.GetCustomAttribute(this.GetType(), typeof(T));

    protected bool ValidateValueType(TypedValue? value, out RedisValue? error)
    {
        error = null;
        if (value is null) return true;
        
        var supportsAttribute = GetAttribute<SupportsAttribute>();
        if (supportsAttribute is null) return true;
        
        if (supportsAttribute.StorageType != ValueType.Unknown && supportsAttribute.StorageType != value.Value.Type)
        {
            error = "WRONGTYPE Operation against a key holding the wrong kind of value".ToErrorString();
            return false;
        }
        return true;
    }
}