using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NagasakaEventSystem.Common.RabbitMQService;
using NagasakaEventSystem.Subscribe.Configuration;
using RabbitMQ.Client;

namespace NagasakaEventSystem.Subscribe.Services;

public class MessageSubscriberService : BackgroundService
{
    private readonly ILogger<MessageSubscriberService> _logger;
    private readonly RabbitMQOptions _rabbitMQOptions;
    private readonly IMessageService _messageService;

    public MessageSubscriberService(
        ILogger<MessageSubscriberService> logger,
        IOptions<RabbitMQOptions> rabbitMQOptions,
        IMessageService messageService)
    {
        _logger = logger;
        _rabbitMQOptions = rabbitMQOptions.Value;
        _messageService = messageService;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting MessageSubscriberService...");
        
        try
        {
            await _messageService.connect();
            _logger.LogInformation("Successfully connected to RabbitMQ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");
            throw;
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MessageSubscriberService is running. Queue: {QueueName}", _rabbitMQOptions.QueueName);

        try
        {
            await _messageService.SubscribeToQueue(_rabbitMQOptions.QueueName, OnMessageReceived);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while subscribing to queue");
        }

        // Keep the service running
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private void OnMessageReceived(string message)
    {
        try
        {
            _logger.LogInformation("Message received from ElsaStudio: {Message}", message);
            
            // Additional structured logging with metadata
            _logger.LogInformation("ElsaStudio Message Details: {@MessageDetails}", new
            {
                Message = message,
                ReceivedAt = DateTime.UtcNow,
                Source = "ElsaStudio",
                QueueName = _rabbitMQOptions.QueueName
            });

            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Received: {message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing received message: {Message}", message);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping MessageSubscriberService...");
        
        try
        {
            await _messageService.disconnect();
            _logger.LogInformation("Successfully disconnected from RabbitMQ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while disconnecting from RabbitMQ");
        }

        await base.StopAsync(cancellationToken);
    }
}
