namespace AuthECAPI.Services.RabbitMQ
{
    public interface IRabbitMqPublisherService
    {
        Task PublishMessageAsync(string tenantId, object message);
    }
}
