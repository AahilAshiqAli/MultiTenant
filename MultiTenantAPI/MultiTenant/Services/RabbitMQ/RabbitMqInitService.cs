using RabbitMQ.Client;
using Serilog;

namespace AuthECAPI.Services.RabbitMQ
{   
    public static class RabbitMqInitService
    {
        private static Task<IChannel>? _channelTask;

 
        public static Task<IChannel> GetChannelAsync()
        {
            if (_channelTask != null)
            {
                Log.Information("Returning cached RabbitMQ channel task.");
                return _channelTask;
            }

            Log.Information("Creating and caching new RabbitMQ channel task.");
            _channelTask = CreateAndCacheChannelAsync();
            return _channelTask;
        }

        private static async Task<IChannel> CreateAndCacheChannelAsync()
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
                var channel = await connection.CreateChannelAsync();
                Log.Information("RabbitMQ channel created.");

                // Declare queues once
                Log.Information("Declaring queue: tasks.high");
                await channel.QueueDeclareAsync("tasks.high", durable: true, exclusive: false, autoDelete: false);

                Log.Information("Declaring queue: tasks.normal");
                await channel.QueueDeclareAsync("tasks.normal", durable: true, exclusive: false, autoDelete: false);

                Log.Information("Declaring queue: tasks.low");
                await channel.QueueDeclareAsync("tasks.low", durable: true, exclusive: false, autoDelete: false);

                Log.Information("All RabbitMQ queues declared successfully.");

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
