using RabbitMQ.Client;
using Serilog;
using System.Threading.Channels;

namespace MultiTenantAPI.Services.RabbitMQ
{   
    public static class RabbitMqInitService
    {
        private static Task<IChannel>? _channelTaskHigh;
        private static Task<IChannel>? _channelTaskNormal;
        private static Task<IChannel>? _channelTaskLow;

        public static Task<IChannel> GetChannelAsync(string queue)
        {
            return queue switch
            {
                "tasks.high" => _channelTaskHigh ??= CreateAndCacheChannelAsync("tasks.high"),
                "tasks.normal" => _channelTaskNormal ??= CreateAndCacheChannelAsync("tasks.normal"),
                "tasks.low" => _channelTaskLow ??= CreateAndCacheChannelAsync("tasks.low"),
                _ => throw new ArgumentException($"Unsupported queue: {queue}")
            };
        }

        private static async Task<IChannel> CreateAndCacheChannelAsync(string queue)
        {
            try
            {
                Log.Information("Initializing RabbitMQ connection factory.");
                var factory = new ConnectionFactory
                {
                    HostName = "localhost",
                    UserName = "guest",
                    Password = "guest"
                };

                Log.Information("Creating RabbitMQ connection asynchronously.");
                var connection = await factory.CreateConnectionAsync();
                Log.Information("RabbitMQ connection established.");

                Log.Information("Creating RabbitMQ channel asynchronously.");
                IChannel channel;

                switch (queue)
                {
                    case "tasks.high":
                        channel = await connection.CreateChannelAsync();
                        break;

                    case "tasks.normal":
                        channel = await connection.CreateChannelAsync();
                        break;

                    case "tasks.low":
                        channel = await connection.CreateChannelAsync();
                        break;

                    default:
                        throw new ArgumentException($"Unsupported queue: {queue}");
                }
                Log.Information("RabbitMQ channel created.");

                switch (queue)
                {
                    case "tasks.high":
                        Log.Information("Declaring queue: tasks.high");
                        await channel.QueueDeclareAsync("tasks.high", durable: true, exclusive: false, autoDelete: false);
                        break;

                    case "tasks.normal":
                        Log.Information("Declaring queue: tasks.normal");
                        await channel.QueueDeclareAsync("tasks.normal", durable: true, exclusive: false, autoDelete: false);
                        break;

                    case "tasks.low":
                        Log.Information("Declaring queue: tasks.low");
                        await channel.QueueDeclareAsync("tasks.low", durable: true, exclusive: false, autoDelete: false);
                        break;

                    default:
                        throw new ArgumentException($"Unsupported queue name: {queue}");
                }

                return channel;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while initializing RabbitMQ channel.");
                throw;
            }
        }
    }
}
