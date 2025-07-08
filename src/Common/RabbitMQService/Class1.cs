using System;
using System.Text;
using System.Threading.Channels;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Hosting;

namespace NagasakaEventSystem.Common.RabbitMQService;

public interface IMessageService
{
    Task connect();
    Task disconnect();
    Task PublishMessage(string message);
    Task SubscribeToQueue(string queueName, Action<string> messageHandler);
}

public class RabbitMQService(IConnectionFactory connectionFactory) : IMessageService, IHostedService, IDisposable
{
    // TODO ロガーを使うように修正する
    private readonly IConnectionFactory _connectionFactory = connectionFactory;
    private IConnection? _connection;
    private IChannel? _channel;

    private readonly string _exchangeName = "Default";
    private readonly string _routingKey = "DefaultRoutingKey";

    public async Task connect()
    {
        Console.WriteLine("Connecting to RabbitMQ...");
        if (_connection == null || !_connection.IsOpen)
        {
            _connection = await _connectionFactory.CreateConnectionAsync();
        }
        if (_channel == null || !_channel.IsOpen)
        {
            _channel = await _connection.CreateChannelAsync();
        }
        await _channel.ExchangeDeclareAsync(_exchangeName, ExchangeType.Fanout);
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
        if (_channel != null && _channel.IsOpen)
        {
            // キューを作成する
            QueueDeclareOk queueDeclareResult = await _channel.QueueDeclareAsync(queue: queueName);
            await _channel.QueueBindAsync(queue: queueDeclareResult.QueueName, exchange: _exchangeName, routingKey: string.Empty);

            // メッセージを受信するためのコンシューマを設定
            var consumer = new AsyncEventingBasicConsumer(_channel);

            // メッセージを受信したときの処理を定義
            consumer.ReceivedAsync += (model, ea) =>
            {
                try
                {
                    byte[] body = ea.Body.ToArray();
                    var receivedMessage = Encoding.UTF8.GetString(body);
                    messageHandler(receivedMessage);

                    // メッセージを確認応答
                    _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing message: {ex.Message}");
                    // エラー時はメッセージを拒否
                    _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                }
                return Task.CompletedTask;
            };

            // コンシューマを開始
            await _channel.BasicConsumeAsync(
                queue: queueDeclareResult.QueueName,
                autoAck: false, // 手動確認応答
                consumer: consumer
            );

            Console.WriteLine($"Started consuming messages from queue: {queueName}");
        }
    }

    public async Task PublishMessage(string message)
    {
        if (_channel != null && _channel.IsOpen)
        {
            Console.WriteLine($"Publishing message: {message}");
            var body = Encoding.UTF8.GetBytes(message);
            await _channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: string.Empty,
                body: body
            );
            Console.WriteLine("Message published successfully.");
        }
        else
        {
            Console.WriteLine("Channel is not open. Cannot publish message.");
            throw new InvalidOperationException("Channel is not open.");
        }
    }

    // IHostedService implementation
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await connect();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await disconnect();
    }

    // IDisposable implementation
    public void Dispose()
    {
        try
        {
            disconnect().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during dispose: {ex.Message}");
        }
    }
}
