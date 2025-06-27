using MultiTenantAPI.Services.FFmpeg;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace MultiTenantAPI.Services.RabbitMQ
{

    public class RabbitMqPublisherService : IRabbitMqPublisherService
    {
        private readonly ILogger<RabbitMqPublisherService> _logger;

        public RabbitMqPublisherService(ILogger<RabbitMqPublisherService> logger)
        {
            _logger = logger;
            _logger.LogInformation("RabbitMqPublisher initialized.");
        }



        public async Task PublishMessageAsync(string tenantId, object message)
        {
            try
            {
                _logger.LogInformation("Publishing message for tenantId: {TenantId}", tenantId);

                var serializedMessage = JsonConvert.SerializeObject(message);
                _logger.LogDebug("Serialized message: {SerializedMessage}", serializedMessage);
                
                var body = Encoding.UTF8.GetBytes(serializedMessage);
                var priority = (byte)TenantPriorityRulesService.GetPriority(tenantId);
                var properties = new BasicProperties
                {
                    Priority = priority
                };

                var routingKey = TenantPriorityRulesService.GetRoutingKey(properties.Priority);
                _logger.LogInformation("RoutingKey: {RoutingKey}, Priority: {Priority}", routingKey, priority);

                var channel = await RabbitMqInitService.GetChannelAsync(routingKey);

                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: routingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: body
                );

                _logger.LogInformation("Message published to queue '{Queue}' for tenantId: {TenantId}", routingKey, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message for tenantId: {TenantId}", tenantId);
                throw;
            }
        }
    }
}
