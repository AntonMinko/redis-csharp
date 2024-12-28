using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection()
    .AddSingleton<Server>()
    .AddSingleton<IStorage, KvpStorage>()
    .AddTransient<IWorker, TcpConnectionWorker>()
    .BuildServiceProvider();

var redisServer = services.GetRequiredService<Server>();
await redisServer.StartAndListen();
