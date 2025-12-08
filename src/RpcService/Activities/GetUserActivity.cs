using Elsa.ServiceBus.MassTransit.Attributes;
using Elsa.ServiceBus.MassTransit.Builders;
using NagasakaEventSystem.RpcService.Messages;

namespace NagasakaEventSystem.RpcService.Activities;

/// <summary>
/// ユーザー取得アクティビティの定義
/// 
/// Configure メソッドをオーバーライドして、
/// Fluent API でカスタマイズする例。
/// </summary>
[RpcActivity(
    DisplayName = "ユーザー取得",
    Description = "ユーザーIDを指定してユーザー情報を取得します。",
    Category = "ユーザー管理",
    DestinationQueue = "GetUser",
    TimeoutSeconds = 30,
    AutoGenerateInputs = false,
    AutoGenerateOutputs = false
)]
public class GetUserActivity : RpcActivityDefinitionBase<GetUserRequest, GetUserResponse>
{
    /// <summary>
    /// Fluent API を使用してアクティビティをカスタマイズ
    /// </summary>
    public override void Configure(RpcActivityBuilder<GetUserRequest, GetUserResponse> builder)
    {
        builder
            // 入力の定義
            .WithInput(r => r.UserId, "ユーザーID", "取得したいユーザーのID")
            
            // 出力の定義
            .WithOutput(r => r.UserId, "ユーザーID", "取得したユーザーのID")
            .WithOutput(r => r.Name, "ユーザー名", "ユーザーの表示名")
            .WithOutput(r => r.Email, "メールアドレス", "ユーザーのメールアドレス");
    }
}
