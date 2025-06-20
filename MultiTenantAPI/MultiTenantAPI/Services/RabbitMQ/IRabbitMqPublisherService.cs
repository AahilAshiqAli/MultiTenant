namespace MultiTenantAPI.Services.RabbitMQ
{
    public interface IRabbitMqPublisherService
    {
        Task PublishMessageAsync(string tenantId, object message);
    }
}
