using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Microsoft.AspNetCore.Mvc;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using NagasakaEventSystem.Common.RabbitMQService;
using RabbitMQ.Client;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime.Options;

public class ActivityResumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ActivityResumerService> _logger;
    private static Dictionary<string, string> _registeredActivityResumers = new();

    public ActivityResumerService(IServiceProvider serviceProvider, ILogger<ActivityResumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public static void registerActivityResumer(string bookmarkName, string resumeCondition)
    {
        // bookmarkNameが重複した場合はログを出力する
        if (_registeredActivityResumers.ContainsKey(bookmarkName))
        {
            Console.WriteLine($"Warning: Bookmark name '{bookmarkName}' is already registered. Overwriting the existing entry.");
        }
        else
        {
            _registeredActivityResumers.Add(bookmarkName, resumeCondition);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // スコープを作成してMessageServiceを取得
        using var scope = _serviceProvider.CreateScope();
        var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();
        
        // await messageService.connect();
        
        await messageService.SubscribeToQueue("ActivityResumerServiceQueue", async (receivedMessage) =>
        {
            try
            {
                // 各メッセージ処理時に新しいスコープを作成
                using var processingScope = _serviceProvider.CreateScope();
                await ProcessMessage(receivedMessage, processingScope.ServiceProvider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {Message}", receivedMessage);
            }
        });
    }

    private async Task ProcessMessage(string receivedMessage, IServiceProvider serviceProvider)
    {
        Console.WriteLine($"Received message: {receivedMessage}");
        
        // 新しいスコープからBookmarkResumerを取得
        var bookmarkResumer = serviceProvider.GetRequiredService<IBookmarkResumer>();
        
        var targetActivities = _registeredActivityResumers.Where(x => x.Value == receivedMessage).ToList();
        
        foreach (var target in targetActivities)
        {
            var bookmarkName = target.Key;
            var resumeCondition = target.Value;

            Console.WriteLine($"Resuming bookmark: {bookmarkName}");
            
            var result = await bookmarkResumer.ResumeAsync<WaitMessage>(bookmarkName, new ResumeBookmarkOptions
            {
                Input = new Dictionary<string, object>
                {
                    { "receivedMessage", receivedMessage }
                },
            });

            if (result.Matched)
            {
                Console.WriteLine($"Successfully resumed bookmark: {bookmarkName}");
                _registeredActivityResumers.Remove(bookmarkName);
            }
            else
            {
                Console.WriteLine($"Bookmark not found: {bookmarkName}");
            }
        }
    }
}

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
    [Input(Description = "Wait for Message")]
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

        // 中断からアクティビティを再開するためのサービスを取得
        // ブックマークを作成してアクティビティを中断
        var bookmarkName = $"WaitMessage_{context.Id}_${Guid.NewGuid()}";
        var bookmark = context.CreateBookmark(bookmarkName, OnMessageReceived);
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<WaitMessage>();
        ActivityResumerService.registerActivityResumer(bookmark.Id, Message);

        // await context.CompleteActivityAsync();
    }

    // ブックマークが再開されたときに呼び出されるコールバック
    private async ValueTask OnMessageReceived(ActivityExecutionContext context)
    {
        Console.WriteLine("WaitMessage activity resumed!");
        // アクティビティを完了
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
                .AddActivitiesFrom<Program>()
                .AddWorkflowsFrom<Program>()
            );

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

        // DIコンテナにRabbitMQのサービスを登録
        services.AddSingleton<IMessageService, RabbitMQService>();
        // RabbitMQServiceをHostedServiceとしても登録（同じインスタンスを使用）
        services.AddHostedService<RabbitMQService>(provider => 
            (RabbitMQService)provider.GetRequiredService<IMessageService>());

        // DIコンテナにアクティビティリズマーサービスを登録
        services.AddHostedService<ActivityResumerService>();


        // CORSの設定
        services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().WithExposedHeaders("*")));
        // MVCの設定
        services.AddRazorPages(options => options.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute()));

        // DIコンテナをビルド
        var app = builder.Build();
        // RabbitMQの接続を開始
        // var messageService = services..GetRequiredService<IMessageService>();
        // messageService.connect().GetAwaiter().GetResult();


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
