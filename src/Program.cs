using codecrafters_redis;
using codecrafters_redis.UserSettings;
using Microsoft.Extensions.DependencyInjection;

var userSettingsProvider = new UserSettingsProvider();
await userSettingsProvider.InitializeUserSettingsAsync();
var userSettings = userSettingsProvider.GetUserSettings();

var services = new ServiceCollection()
    .AddSingleton<IUserSettingsProvider>(userSettingsProvider)
    .AddSingleton(userSettings)
    .AddSingleton<Server>()
    .AddSingleton<IStorage, KvpStorage>()
    .AddTransient<ServerInitializer>()
    .AddTransient<IWorker, TcpConnectionWorker>()
    .BuildServiceProvider();

var initializer = services.GetRequiredService<ServerInitializer>();
await initializer.Initialize(args);

var redisServer = services.GetRequiredService<Server>();
await redisServer.StartAndListen();

