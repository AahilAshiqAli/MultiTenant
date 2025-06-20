using AuthECAPI.DTO;
using AuthECAPI.Services.ContentFolder;
using AuthECAPI.Services.CurrentTenant;
using AuthECAPI.Services.RabbitMQ;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

public class RabbitMqConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private IChannel _channel;

    public RabbitMqConsumerService(IServiceProvider serviceProvider, ILogger<RabbitMqConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting RabbitMqConsumer...");

        _channel = await RabbitMqInitService.GetChannelAsync();

        await ConsumeQueueAsync("tasks.high", stoppingToken);
        await ConsumeQueueAsync("tasks.normal", stoppingToken);
        await ConsumeQueueAsync("tasks.low", stoppingToken);
    }

    private async Task ConsumeQueueAsync(string queueName, CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var messageString = Encoding.UTF8.GetString(body);
            _logger.LogInformation("Received message from queue: {QueueName}", queueName);

            try
            {
                var message = JsonConvert.DeserializeObject<ContentMessage>(messageString);
                _logger.LogInformation("Deserialized message: {Message}", messageString);

                using var scope = _serviceProvider.CreateScope();

                var currentTenantService = scope.ServiceProvider.GetRequiredService<ICurrentTenantService>();
                await currentTenantService.SetTenant(message.TenantId);
                _logger.LogInformation("Set tenant: {TenantId}", message.TenantId);

                var contentService = scope.ServiceProvider.GetRequiredService<IContentService>();
                _logger.LogInformation("Processing uploaded content");
                await contentService.ProcessUploadedContentAsync(message);
                _logger.LogInformation("Completed content processing");

                await _channel.BasicAckAsync(ea.DeliveryTag, false);
                _logger.LogInformation("Message acked");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from {QueueName}: {Error}", queueName, ex.Message);
                await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false); // don't leave it unacked
            }
        };


        await _channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);

        // Keep method alive as long as host is running
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}