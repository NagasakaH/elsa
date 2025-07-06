using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Activities.RabbitMq.Extensions;
using Elsa.Options;
using Microsoft.AspNetCore.Mvc;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using NagasakaEventSystem.Common.RabbitMQService;
using RabbitMQ.Client;


// カスタムアクティビティの実装
// 参考 https://docs.elsaworkflows.io/extensibility/custom-activities
// TODO アクティビティは別プロジェクトに分離する(プラグインからの読み込みを検討する)

[Activity("CustomActivity", "Publish a message to Message Queue")]
public class PublishMessage : Activity
{
    [Input(Description = "Message to publish")]
    public string Message { get; set; } = "Hello";
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // RabbitMQのサービスを取得
        var MessageService = context.GetService<IMessageService>();
        if (MessageService == null)
        {
            Console.WriteLine("IMessageService is not registered.");
            throw new InvalidOperationException("IMessageService is not registered.");
        }

        // メッセージをパブリッシュ
        await MessageService.PublishMessage(Message);
        Console.WriteLine("RabbitMQにメッセージをパブリッシュしました。");
        await context.CompleteActivityAsync();
    }
}

[Activity("CustomActivity", "Wait for a message from Message Queue")]
public class WaitMessage : Activity
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // RabbitMQのサービスを取得
        var MessageService = context.GetService<IMessageService>();
        if (MessageService == null)
        {
            Console.WriteLine("IMessageService is not registered.");
            throw new InvalidOperationException("IMessageService is not registered.");
        }

        // TODO メッセージの受信待ち処理を実装する
        await context.CompleteActivityAsync();
    }
}

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

        // DIコンテナにElsaのサービスを登録
        services
            .AddElsa(elsa => elsa
                .UseIdentity(identity =>
                {
                    identity.TokenOptions = options => options.SigningKey = "large-signing-key-for-signing-JWT-tokens"; // TODO: 暫定ハードコーティング、appsettings.jsonから取得するように変更する
                    identity.UseAdminUserProvider();
                })
                .UseDefaultAuthentication()
                .UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef => ef.UseSqlite())) // TODO: SQLServer or PostgreSQLに変更する
                .UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef => ef.UseSqlite())) // TODO: SQLServer or PostgreSQLに変更する
                .UseScheduling()
                .UseJavaScript()
                .UseLiquid()
                .UseCSharp()
                .UseHttp(http => http.ConfigureHttpOptions = options => configuration.GetSection("Http").Bind(options))
                .UseWorkflowsApi()
                .UseMassTransit(massTransit => // massTransitを使用してRabbitMQを設定 // これが期待通りに動いているかはまだ未確認
                {
                    massTransit.UseRabbitMq(
                        "amqp://guest:guest@localhost:5672" // TODO: 暫定ハードコーティング、appsettings.jsonから取得するように変更する
                    );
                })
                .AddActivity<PublishMessage>() // PublishMessageアクティビティを追加
                .AddActivity<WaitMessage>() // WaitMessageアクティビティを追加
                .AddActivitiesFrom<Elsa.Activities.RabbitMq.RabbitMqMessageReceived>()
                .AddActivitiesFrom<Program>()
                .AddWorkflowsFrom<Program>()
            );

        // DIコンテナにRabbitMQのサービスを登録
        services.AddSingleton<IMessageService, RabbitMQService>();
        // DIコンテナにRabbitMQの接続設定を登録
        var connectionFactory = new ConnectionFactory // TODO : 暫定ハードコーティング、appsettings.jsonから取得するように変更する
        {
            HostName = "localhost", // RabbitMQのホスト名
            Port = 5672, // RabbitMQのデフォルトポート
            UserName = "guest", // RabbitMQのデフォルトユーザー名
            Password = "guest", // RabbitMQのデフォルトパスワード
            VirtualHost = "/" // RabbitMQのデフォルト仮想ホスト
        };
        services.AddSingleton<IConnectionFactory>(connectionFactory);
        
        // CORSの設定
        services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().WithExposedHeaders("*")));
        // MVCの設定
        services.AddRazorPages(options => options.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute()));

        // DIコンテナをビルド
        var app = builder.Build();

        // RabbitMQの接続を開始
        var messageService = app.Services.GetRequiredService<IMessageService>();
        messageService.connect().GetAwaiter().GetResult();

        // 開発環境固有の設定を有効化
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
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
