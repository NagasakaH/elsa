using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NagasakaEventSystem.RpcService.Consumers;

namespace NagasakaEventSystem.RpcService;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("RPC Service を起動中...");

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddMassTransit(bus =>
                {
                    // コンシューマーを登録
                    bus.AddConsumer<GetUserConsumer>();
                    bus.AddConsumer<CalculateConsumer>();
                    bus.AddConsumer<SearchProductsConsumer>();

                    bus.UsingRabbitMq((ctx, cfg) =>
                    {
                        // RabbitMQ接続設定
                        cfg.Host("localhost", 5672, "/", h =>
                        {
                            h.Username("guest");
                            h.Password("guest");
                        });

                        // MassTransitのデフォルト命名規則を使用してエンドポイントを自動構成
                        // これにより IRequestClient<T> からのリクエストが正しくルーティングされる
                        cfg.ConfigureEndpoints(ctx);
                    });
                });
            })
            .Build();

        Console.WriteLine("RabbitMQ に接続中...");
        
        await host.StartAsync();

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("  RPC Service が起動しました");
        Console.WriteLine("========================================");
        Console.WriteLine();
        Console.WriteLine("利用可能なRPCコンシューマー:");
        Console.WriteLine("  - GetUserConsumer    : ユーザー情報取得");
        Console.WriteLine("  - CalculateConsumer  : 計算処理");
        Console.WriteLine();
        Console.WriteLine("終了するには Ctrl+C を押してください...");
        Console.WriteLine();

        // Ctrl+Cで終了するまで待機
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            Console.WriteLine("\nシャットダウン中...");
        };

        try
        {
            await Task.Delay(Timeout.Infinite, cts.Token);
        }
        catch (TaskCanceledException)
        {
            // 正常終了
        }

        await host.StopAsync();
        Console.WriteLine("RPC Service を終了しました。");
    }
}
