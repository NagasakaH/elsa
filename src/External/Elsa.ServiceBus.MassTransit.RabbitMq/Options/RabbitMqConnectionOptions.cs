namespace Elsa.ServiceBus.MassTransit.RabbitMq.Options;

/// <summary>
/// Options for configuring the RabbitMQ connection.
/// </summary>
public class RabbitMqConnectionOptions
{
    /// <summary>
    /// The RabbitMQ host name or IP address.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// The RabbitMQ port number.
    /// </summary>
    public ushort Port { get; set; } = 5672;

    /// <summary>
    /// The RabbitMQ virtual host.
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// The username for authentication.
    /// </summary>
    public string Username { get; set; } = "guest";

    /// <summary>
    /// The password for authentication.
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Whether to use SSL/TLS for the connection.
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// Gets the connection URI based on the configured options.
    /// </summary>
    public Uri GetConnectionUri()
    {
        var scheme = UseSsl ? "amqps" : "amqp";
        var encodedVirtualHost = Uri.EscapeDataString(VirtualHost);
        return new Uri($"{scheme}://{Username}:{Password}@{Host}:{Port}/{encodedVirtualHost}");
    }
}
