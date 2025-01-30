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
        if (kvp.ContainsKey("--dir"))
        {
            userSettings.Persistence.Dir = kvp["--dir"];
            await userSettingsProvider.SaveUserSettingsAsync(userSettings);
        }

        if (kvp.ContainsKey("--dbfilename"))
        {
            userSettings.Persistence.DbFileName = kvp["--dbfilename"];
            await userSettingsProvider.SaveUserSettingsAsync(userSettings);
        }

        if (kvp.ContainsKey("--port"))
        {
            userSettings.Runtime.Port = int.Parse(kvp["--port"]);
        }
        
        var dir = userSettings.Persistence.Dir;
        var dbFileName = userSettings.Persistence.DbFileName;
        await LoadFromBackupFile(dir, dbFileName);
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