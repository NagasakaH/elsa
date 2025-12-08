using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using NagasakaEventSystem.RpcService.Consumers;

namespace NagasakaEventSystem.RpcService.Extensions;

/// <summary>
/// RPCサービスの拡張メソッド
/// </summary>
public static class RpcServiceExtensions
{
    /// <summary>
    /// RPCコンシューマーをMassTransitに登録します
    /// </summary>
    /// <param name="configurator">MassTransitのバス構成</param>
    public static void AddRpcConsumers(this IBusRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<GetUserConsumer>();
        configurator.AddConsumer<CalculateConsumer>();
    }

    /// <summary>
    /// RPCエンドポイントを構成します（デフォルトの命名規則を使用）
    /// </summary>
    /// <param name="configurator">RabbitMQバス構成</param>
    /// <param name="context">バス登録コンテキスト</param>
    public static void ConfigureRpcEndpoints(this IRabbitMqBusFactoryConfigurator configurator, IBusRegistrationContext context)
    {
        // MassTransitのデフォルト命名規則を使用してエンドポイントを自動構成
        // これにより IRequestClient<T> からのリクエストが正しくルーティングされる
        configurator.ConfigureEndpoints(context);
    }
}
