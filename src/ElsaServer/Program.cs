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
                .UseMassTransit(massTransit => // massTransitを使用してRabbitMQを設定 // これが期待通りに動いているかはまだ未確認
                {
                    massTransit.UseRabbitMq(
                        "amqp://guest:guest@localhost:5672" // TODO: 暫定ハードコーティング、appsettings.jsonから取得するように変更する
                    );
                    // テスト用シンプルメッセージタイプを登録
                    massTransit.AddMessageType<TestMessage>();
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
