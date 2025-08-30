using codecrafters_redis.Persistence;
using codecrafters_redis.UserSettings;

namespace codecrafters_redis;

internal class ServerInitializer(IUserSettingsProvider userSettingsProvider, IStorage storage)
{
    internal async Task Initialize(string[] args)
    {
        await userSettingsProvider.InitializeUserSettingsAsync();
        var userSettings = userSettingsProvider.GetUserSettings();

        var kvp = ParseArgs(args);
        await HandleDirSetting(kvp, userSettings);
        await HandleDbFilenameSetting(kvp, userSettings);
        await HandlePortSetting(kvp, userSettings);
        await HandleReplicaOfSetting(kvp, userSettings);
        
        await LoadFromBackupFile(userSettings.Persistence.Dir, userSettings.Persistence.DbFileName);
    }

    private async Task HandleReplicaOfSetting(Dictionary<string, string> kvp, Settings userSettings)
    {
        if (!kvp.TryGetValue("--replicaof", out var replicaOf)) return;

        try
        {
            var parts = replicaOf.Split(' ');
            var masterHost = parts[0];
            var masterPort = int.Parse(parts[1]);

            userSettings.Replication.Role = ReplicationRole.Slave;
            userSettings.Replication.ReplicaOf = new ReplicaOfSettings {MasterHost = masterHost, MasterPort = masterPort};
        }
        catch (Exception e)
        {
            Console.WriteLine($" Unable to parse --replicaOf value: {e}");

            userSettings.Replication.Role = ReplicationRole.Master;
        }
    }

    private static async Task HandlePortSetting(Dictionary<string, string> kvp, Settings userSettings)
    {
        if (!kvp.TryGetValue("--port", out var port)) return;
        userSettings.Runtime.Port = int.Parse(port);
    }

    private async Task HandleDbFilenameSetting(Dictionary<string, string> kvp, Settings userSettings)
    {
        if (!kvp.TryGetValue("--dbfilename", out var dbFilename)) return;

        userSettings.Persistence.DbFileName = dbFilename;
        await userSettingsProvider.SaveUserSettingsAsync(userSettings);
    }

    private async Task HandleDirSetting(Dictionary<string, string> kvp, Settings userSettings)
    {
        if (!kvp.TryGetValue("--dir", out var dir)) return;
        
        userSettings.Persistence.Dir = dir;
        await userSettingsProvider.SaveUserSettingsAsync(userSettings);
    }

    private async Task LoadFromBackupFile(string dir, string dbFileName)
    {
        var backupFile = Path.Combine(dir, dbFileName);
        if (!File.Exists(backupFile)) return;

        var parser = new RdbParser();
        try
        {
            var dataModel = await parser.ParseAsync(backupFile);
            if (dataModel.Databases.Count == 0) return;

            var loadedData = dataModel.Databases[0];
            storage.Initialize(loadedData);

            Console.WriteLine($"Loaded {loadedData.Count} records");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private Dictionary<string, string> ParseArgs(string[] args)
    {
        var map = new Dictionary<string, string>();
        for (int i = 0; i < args.Length; i += 2)
        {
            if (args.Length < i + 2) break;
        
            string key = args[i];
            string value = args[i + 1];
            map.Add(key, value);
        }
    
        return map;
    }
}