using Microsoft.Extensions.DependencyInjection;

var userSettingsProvider = new UserSettingsProvider();
await userSettingsProvider.InitializeUserSettingsAsync();
var userSettings = userSettingsProvider.GetUserSettings();

var services = new ServiceCollection()
    .AddSingleton<IUserSettingsProvider>(userSettingsProvider)
    .AddSingleton(userSettings)
    .AddSingleton<Server>()
    .AddSingleton<IStorage, KvpStorage>()
    .AddSingleton<ReplicaManager>()
    .AddSingleton<MasterManager>()
    .AddTransient<ServerInitializer>()
    .AddTransient<IWorker, TcpConnectionWorker>()
    .AddTransient<CommandHandler>()
    .BuildServiceProvider();

var initializer = services.GetRequiredService<ServerInitializer>();
await initializer.Initialize(args);

var masterManager = services.GetRequiredService<MasterManager>();
masterManager.StartReplication();

var replicationManager = services.GetRequiredService<ReplicaManager>();
await replicationManager.ConnectToMaster();

var redisServer = services.GetRequiredService<Server>();
await redisServer.StartAndListen();

