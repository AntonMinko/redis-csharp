using codecrafters_redis;
using codecrafters_redis.UserSettings;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection()
    .AddSingleton<IUserSettingsProvider, UserSettingsProvider>()
    .AddSingleton<Server>()
    .AddSingleton<IStorage, KvpStorage>()
    .AddTransient<ServerInitializer>()
    .AddTransient<IWorker, TcpConnectionWorker>()
    .BuildServiceProvider();

var initializer = services.GetRequiredService<ServerInitializer>();
await initializer.Initialize(args);

var redisServer = services.GetRequiredService<Server>();
await redisServer.StartAndListen();

