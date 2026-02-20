using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.MassTransit.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Notifications;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using ElsaServer.Activities;
using ElsaServer.Consumers;
using ElsaServer.Hosted;
using ElsaServer.Messages;
using ElsaServer.Options;
using ElsaServer.Services;
using NagasakaEventSystem.Activities.Loader;
using NagasakaEventSystem.WorkflowCatalog;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(args.Length);

        // DIコンテナの準備
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseStaticWebAssets();
        var services = builder.Services;
        var configuration = builder.Configuration;

        // PostgreSQL接続文字列
        var postgresConnectionString = configuration.GetConnectionString("PostgreSQL")
            ?? "Host=localhost;Port=5432;Database=elsa_workflows;Username=elsa_user;Password=elsa_password";

        // MassTransit設定セクション
        var massTransitSection = configuration.GetSection("MassTransit");
        var rabbitMqHost = massTransitSection.GetValue<string>("Host") ?? "localhost";
        var rabbitMqPort = massTransitSection.GetValue<int?>("Port") ?? 5672;
        var rabbitMqVHost = massTransitSection.GetValue<string>("VirtualHost") ?? "/";
        var rabbitMqUsername = massTransitSection.GetValue<string>("Username") ?? "guest";
        var rabbitMqPassword = massTransitSection.GetValue<string>("Password") ?? "guest";
        var rabbitMqUri = $"amqp://{rabbitMqUsername}:{rabbitMqPassword}@{rabbitMqHost}:{rabbitMqPort}{rabbitMqVHost}";

        // オプション設定バインド
        services.Configure<WorkflowCatalogOptions>(configuration.GetSection("WorkflowCatalog"));
        services.Configure<ActivityAssemblyOptions>(configuration.GetSection("Activities"));
        services.Configure<WorkflowBusOptions>(configuration.GetSection("WorkflowMessaging"));

        // DIコンテナにElsaのサービスを登録
        services
            .AddElsa(elsa => elsa
                .AddActivitiesFrom<PublishWorkflowStatus>()
                .UseIdentity(identity =>
                {
                    identity.TokenOptions = options => options.SigningKey = "large-signing-key-for-signing-JWT-tokens"; // TODO: 暫定ハードコーティング、appsettings.jsonから取得するように変更する
                    identity.UseAdminUserProvider();
                })
                .UseDefaultAuthentication()
                .UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef => ef.UsePostgreSql(postgresConnectionString)))
                .UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef => ef.UsePostgreSql(postgresConnectionString)))
                .UseScheduling()
                .UseJavaScript()
                .UseLiquid()
                .UseCSharp()
                .UseHttp(http => http.ConfigureHttpOptions = options => configuration.GetSection("Http").Bind(options))
                .UseWorkflowsApi()
                .UseMassTransit(massTransit =>
                {
                    // Elsa標準のMassTransit拡張でRabbitMQを設定
                    massTransit.UseRabbitMq(rabbitMqUri);
                })
            );

        // MassTransitでConsumerを登録（StartWorkflowConsumer）
        services.AddMassTransit(busConfig =>
        {
            busConfig.AddConsumer<StartWorkflowConsumer, StartWorkflowConsumerDefinition>();

            busConfig.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqHost, (ushort)rabbitMqPort, rabbitMqVHost, h =>
                {
                    h.Username(rabbitMqUsername);
                    h.Password(rabbitMqPassword);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        // サービス登録
        services.AddSingleton<RunContextStore>();
        services.AddSingleton<WorkflowCatalog>();
        services.AddScoped<WorkflowCatalogLoader>();
        services.AddScoped<ActivityAssemblyLoader>();

        // Directory activity loader (loads DLLs from configured folder and registers into Elsa).
        services.AddScoped<IActivityRegistrar, ElsaActivityRegistrar>();
        services.AddScoped<DirectoryActivityAssemblyLoader>();
        services.AddSingleton<IPayloadMapper, DefaultPayloadMapper>();
        services.AddScoped<IWorkflowLauncher, WorkflowLauncher>();
        services.AddScoped<WorkflowStatusPublisher>();
        services.AddScoped<IWorkflowStatusPublisher>(sp => sp.GetRequiredService<WorkflowStatusPublisher>());
        services.AddScoped<INotificationHandler<WorkflowStarted>>(sp => sp.GetRequiredService<WorkflowStatusPublisher>());
        services.AddScoped<INotificationHandler<WorkflowExecuted>>(sp => sp.GetRequiredService<WorkflowStatusPublisher>());
        services.AddScoped<INotificationHandler<WorkflowFinished>>(sp => sp.GetRequiredService<WorkflowStatusPublisher>());
        services.AddHostedService<StartupInitializationHostedService>();

        // CORSの設定
        services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().WithExposedHeaders("*")));
        // MVCの設定
        services.AddRazorPages(options => options.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute()));

        // DIコンテナをビルド
        var app = builder.Build();

        // 開発環境固有の設定を有効化
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
            app.UseHttpsRedirection();
        }
        app.UseBlazorFrameworkFiles();
        app.UseRouting();
        app.UseCors();
        app.UseStaticFiles();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseWorkflowsApi();
        app.UseWorkflows();
        app.MapFallbackToPage("/_Host");
        app.Run();
    }
}
