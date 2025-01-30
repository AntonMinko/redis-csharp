namespace codecrafters_redis.UserSettings;

public class PersistenceSettings
{
    public required string Dir { get; set; } = null!;
    
    public required string DbFileName { get; set; } = null!;
}

public class Settings
{
    public static Settings Default { get; } = new()
    {
        Persistence = new PersistenceSettings
        {
            DbFileName = "backup.rdb",
            Dir = GetAppDataDir()
        },
        Runtime = new RuntimeSettings
        {
            Port = 6379
        },
        Replication = new ReplicationSettings
        {
            Role = ReplicationRole.Master
        }
    };
    
    public required PersistenceSettings Persistence { get; init; }
    
    public required RuntimeSettings Runtime { get; init; }
    
    public required ReplicationSettings Replication { get; init; }

    internal static string GetAppDataDir() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyRedis");
}

public class RuntimeSettings
{
    public required int Port { get; set; }
}

public class ReplicationSettings
{
    public required ReplicationRole Role { get; set; }
}

public enum ReplicationRole
{
    Master,
    Slave
}