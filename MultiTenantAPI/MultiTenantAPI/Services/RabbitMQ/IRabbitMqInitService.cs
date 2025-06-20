using RabbitMQ.Client;

namespace MultiTenantAPI.Services.RabbitMQ
{
    public interface IRabbitMqInitService
    {
        public Task<IChannel> GetChannelAsync();

    
    }
}
