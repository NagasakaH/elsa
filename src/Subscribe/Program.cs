using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NagasakaEventSystem.Common.RabbitMQService;
using NagasakaEventSystem.Subscribe.Configuration;
using NagasakaEventSystem.Subscribe.Services;
using RabbitMQ.Client;
using Serilog;

namespace NagasakaEventSystem.Subscribe;

class Program
{
    static async Task Main(string[] args)
    {
        // Setup configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        // Setup Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        try
        {
            Log.Information("Starting NagasakaEventSystem Subscribe Service");

            // Create host builder
            var host = Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {
                    // Configuration options
                    services.Configure<RabbitMQOptions>(configuration.GetSection(RabbitMQOptions.SectionName));

                    // RabbitMQ connection factory
                    services.AddSingleton<IConnectionFactory>(serviceProvider =>
                    {
                        var rabbitMQOptions = configuration.GetSection(RabbitMQOptions.SectionName).Get<RabbitMQOptions>()!;
                        return new ConnectionFactory
                        {
                            HostName = rabbitMQOptions.HostName,
                            Port = rabbitMQOptions.Port,
                            UserName = rabbitMQOptions.UserName,
                            Password = rabbitMQOptions.Password,
                            VirtualHost = rabbitMQOptions.VirtualHost
                        };
                    });

                    // RabbitMQ service
                    services.AddSingleton<IMessageService, RabbitMQService>();

                    // Background service
                    services.AddHostedService<MessageSubscriberService>();
                })
                .UseConsoleLifetime()
                .Build();

            // Run the host
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}