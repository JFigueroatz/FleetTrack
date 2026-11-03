using FleetTrack.Consumer;
using Npgsql;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(NpgsqlDataSource.Create(
    builder.Configuration.GetConnectionString("Postgres") ?? throw new InvalidOperationException("Falta ConnectionStrings:Postgres.")));
builder.Services.AddStackExchangeRedisCache(options =>
{
    var redis = ConfigurationOptions.Parse(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379");
    redis.AbortOnConnectFail = false;
    redis.ConnectTimeout = 1000;
    redis.SyncTimeout = 1000;
    redis.AsyncTimeout = 1000;
    options.ConfigurationOptions = redis;
});
builder.Services.AddSingleton<EventProcessor>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
