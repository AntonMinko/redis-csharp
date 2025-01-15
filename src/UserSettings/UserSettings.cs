namespace codecrafters_redis.UserSettings;

public class PersistenceSettings
{
    public required string Dir { get; set; } = null!;
    
    public required string DbFileName { get; set; } = null!;
}

public class UserSettings
{
    public static UserSettings Default { get; } = new()
    {
        Persistence = new PersistenceSettings
        {
            DbFileName = "backup.rdb",
            Dir = GetAppDataDir()
        }
    };
    
    public required PersistenceSettings Persistence { get; init; }

    internal static string GetAppDataDir() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyRedis");
}