using System;
using System.Text;
using RabbitMQ.Client;

namespace NagasakaEventSystem.Common.RabbitMQService;

public interface IMessageService
{
    Task connect();
    Task disconnect();
    Task PublishMessage(string message);
    Task SubscribeToQueue(string queueName, Action<string> messageHandler);
}

public class RabbitMQService(IConnectionFactory connectionFactory) : IMessageService
{
    // TODO ロガーを使うように修正する
    private readonly IConnectionFactory _connectionFactory = connectionFactory;
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task connect()
    {
        Console.WriteLine("Connecting to RabbitMQ...");
        _connection =  await _connectionFactory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();
        Console.WriteLine("Connected to RabbitMQ.");
    }

    public async Task disconnect()
    {
        if (_channel != null && _channel.IsOpen)
        {
            await _channel.CloseAsync();
        }
        if (_connection != null && _connection.IsOpen)
        {
            await _connection.CloseAsync();
        }
    }

    public async Task SubscribeToQueue(string queueName, Action<string> messageHandler)
    {
        Console.WriteLine($"Subscribing to queue: {queueName}");
    }
    
    public async Task PublishMessage(string message)
    {
        Console.WriteLine($"Publishing message: {message}");
    }
}
