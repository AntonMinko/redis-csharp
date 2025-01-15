using codecrafters_redis;
using codecrafters_redis.UserSettings;
using Microsoft.Extensions.DependencyInjection;

var userSettingsProvider = new UserSettingsProvider();
await userSettingsProvider.InitializeUserSettingsAsync();
var userSettings = userSettingsProvider.GetUserSettings();

var kvp = ParseArgs();
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

Dictionary<string, string> ParseArgs()
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

var services = new ServiceCollection()
    .AddSingleton<IUserSettingsProvider>(userSettingsProvider)
    .AddSingleton<Server>()
    .AddSingleton<IStorage, KvpStorage>()
    .AddTransient<IWorker, TcpConnectionWorker>()
    .BuildServiceProvider();

var redisServer = services.GetRequiredService<Server>();
await redisServer.StartAndListen();
