using codecrafters_redis.Commands;
using codecrafters_redis.Commands.Handlers;
using codecrafters_redis.Server;
using codecrafters_redis.Storage;
using codecrafters_redis.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
using Type = codecrafters_redis.Commands.Handlers.Type;

var userSettingsProvider = new UserSettingsProvider();
await userSettingsProvider.InitializeUserSettingsAsync();
var userSettings = userSettingsProvider.GetUserSettings();

var serviceBuilder = new ServiceCollection()
    .AddSingleton<IUserSettingsProvider>(userSettingsProvider)
    .AddSingleton(userSettings)
    .AddSingleton<Server>()
    .AddSingleton<KvpStorage>()
    .AddSingleton<ListStorage>()
    .AddSingleton<StorageManager>()
    .AddSingleton<ReplicaManager>()
    .AddSingleton<MasterManager>()
    .AddSingleton<PubSub>()
    .AddTransient<ServerInitializer>()
    .AddTransient<IWorker, TcpConnectionWorker>()
    .AddTransient<Processor>();

serviceBuilder
    .AddTransient<ICommandHandler, BLPop>()
    .AddTransient<ICommandHandler, Config>()
    .AddTransient<ICommandHandler, Echo>()
    .AddTransient<ICommandHandler, Get>()
    .AddTransient<ICommandHandler, Info>()
    .AddTransient<ICommandHandler, Keys>()
    .AddTransient<ICommandHandler, LLen>()
    .AddTransient<ICommandHandler, LPop>()
    .AddTransient<ICommandHandler, LPush>()
    .AddTransient<ICommandHandler, LRange>()
    .AddTransient<ICommandHandler, Ping>()
    .AddTransient<ICommandHandler, PSync>()
    .AddTransient<ICommandHandler, Publish>()
    .AddTransient<ICommandHandler, ReplConf>()
    .AddTransient<ICommandHandler, RPush>()
    .AddTransient<ICommandHandler, Set>()
    .AddTransient<ICommandHandler, Subscribe>()
    .AddTransient<ICommandHandler, Type>()
    .AddTransient<ICommandHandler, Unsubscribe>()
    .AddTransient<ICommandHandler, Wait>();

var services = serviceBuilder
    .BuildServiceProvider();

var initializer = services.GetRequiredService<ServerInitializer>();
await initializer.Initialize(args);

var masterManager = services.GetRequiredService<MasterManager>();
masterManager.StartReplication();

var replicationManager = services.GetRequiredService<ReplicaManager>();
await replicationManager.ConnectToMaster();

var redisServer = services.GetRequiredService<Server>();
await redisServer.StartAndListen();

