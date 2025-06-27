using MultiTenantAPI.DTO;
using MultiTenantAPI.Services.ContentFolder;
using MultiTenantAPI.Services.ContentProcessor;
using MultiTenantAPI.Services.CurrentTenant;
using MultiTenantAPI.Services.RabbitMQ;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using System.Text;

public class RabbitMqConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqConsumerService> _logger;

    public RabbitMqConsumerService(IServiceProvider serviceProvider, ILogger<RabbitMqConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var allTasks = new List<Task>();

        allTasks.Add(StartConsumersAsync("tasks.high", 4, stoppingToken));
        allTasks.Add(StartConsumersAsync("tasks.normal", 2, stoppingToken));
        allTasks.Add(StartConsumersAsync("tasks.low", 1, stoppingToken));

        await Task.WhenAll(allTasks);
    }

    private async Task StartConsumersAsync(string queueName, int consumerCount, CancellationToken stoppingToken)
    {
        var consumerTasks = new List<Task>();

        for (int i = 0; i < consumerCount; i++)
        {
            var channel = await RabbitMqInitService.GetChannelAsync(queueName);
            consumerTasks.Add(ConsumeQueueAsync(channel, queueName, stoppingToken));
        }

        // Await all consumers for this queue
        await Task.WhenAll(consumerTasks);
    }


    private async Task ConsumeQueueAsync(IChannel channel, string queueName, CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(channel);

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

                var contentProcessingService = scope.ServiceProvider.GetRequiredService<IContentProcessorService>();
                _logger.LogInformation("Processing uploaded content");
                await contentProcessingService.ProcessUploadedContentAsync(message);
                _logger.LogInformation("Completed content processing");

                await channel.BasicAckAsync(ea.DeliveryTag, false);
                _logger.LogInformation("Message acked");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from {QueueName}: {Error}", queueName, ex.Message);
                await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
            }
        };

        await channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);

        // Keep alive while cancellation is not requested
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}
