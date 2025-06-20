using RabbitMQ.Client;

namespace AuthECAPI.Services.RabbitMQ
{
    public interface IRabbitMqInitService
    {
        public Task<IChannel> GetChannelAsync();

    
    }
}
