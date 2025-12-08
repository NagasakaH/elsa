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
using ElsaServer.Messages;
using NagasakaEventSystem.RpcService.Messages;
using NagasakaEventSystem.RpcService.Extensions;
using Elsa.ServiceBus.MassTransit.Extensions;

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

        // DIコンテナにElsaのサービスを登録
        services
            .AddElsa(elsa => elsa
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
                .UseMassTransit(massTransit => // massTransitを使用してRabbitMQを設定
                {
                    // RabbitMQの設定（VirtualHost対応）
                    massTransit.UseRabbitMq(options =>
                    {
                        options.Host = "localhost";
                        options.Port = 5672;
                        options.VirtualHost = "/"; // Virtual Hostを指定可能
                        options.Username = "guest";
                        options.Password = "guest";
                    });

                    // テスト用シンプルメッセージタイプを登録
                    massTransit.AddMessageType<TestMessage>();

                    // RPC用メッセージタイプを登録（リクエスト/レスポンスペア）
                    // 宛先アドレスを指定（RpcServiceで実行されるConsumerのキュー名）
                    // MassTransitのデフォルト命名規則（PascalCase、"Consumer"サフィックス除去）:
                    //   GetUserConsumer -> "GetUser"
                    //   CalculateConsumer -> "Calculate"
                    massTransit.AddRpcMessageType<GetUserRequest, GetUserResponse>("queue:GetUser", TimeSpan.FromSeconds(30));
                    massTransit.AddRpcMessageType<CalculateRequest, CalculateResponse>("queue:Calculate", TimeSpan.FromSeconds(30));

                    // ========================================
                    // 方法1: アセンブリスキャンによる自動登録
                    // ========================================
                    // [RpcActivity] 属性を持つクラスを自動的に検出して登録
                    // RpcServiceプロジェクトの Activities フォルダ内のクラスを自動登録
                    massTransit.AddRpcActivitiesFromAssemblyContaining<CreateOrderRequest>();

                    // ========================================
                    // 方法2: 個別クラスを直接登録
                    // ========================================
                    // massTransit.AddRpcActivity<CreateOrderActivity>();
                    // massTransit.AddRpcActivity<CalculateActivity>();
                    // massTransit.AddRpcActivity<GetUserActivity>();

                    // ========================================
                    // 方法3: Fluent Builder による詳細なカスタマイズ
                    // ========================================
                    // 以下はFluent APIを使った例（コメントアウト）
                    // 上記のアセンブリスキャンと同等の機能をコードで記述
                    /*
                    massTransit.AddRpcActivity<CreateOrderRequest, CreateOrderResponse>(builder => builder
                        .WithTypeName("Custom.CreateOrder")
                        .WithDisplayName("注文作成")
                        .WithDescription("顧客の注文を作成します。")
                        .WithCategory("注文管理")
                        .WithDestination("CreateOrder")
                        .WithTimeout(TimeSpan.FromSeconds(60))
                        .WithInput(r => r.OrderId, "注文ID")
                        .WithInput(r => r.Customer.Name, "顧客名")
                        .WithCollectionInput(r => r.Items, "注文明細")
                        .WithOutput(r => r.Success, "成功")
                        .WithOutput(r => r.OrderNumber, "注文番号")
                    );
                    */
                })
            );

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
