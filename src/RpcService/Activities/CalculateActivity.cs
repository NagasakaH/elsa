using Elsa.ServiceBus.MassTransit.Attributes;
using Elsa.ServiceBus.MassTransit.Builders;
using NagasakaEventSystem.RpcService.Messages;

namespace NagasakaEventSystem.RpcService.Activities;

/// <summary>
/// 計算アクティビティの定義
/// 
/// シンプルなRPCアクティビティの例。
/// AutoGenerateInputs/Outputs = true の場合、
/// 明示的に定義されていないプロパティは自動的に生成されます。
/// </summary>
[RpcActivity(
    DisplayName = "計算実行",
    Description = "2つの数値で計算を実行します。",
    Category = "計算",
    DestinationQueue = "Calculate",
    TimeoutSeconds = 30,
    AutoGenerateInputs = true,  // リクエストから自動生成
    AutoGenerateOutputs = true  // レスポンスから自動生成
)]
public class CalculateActivity : RpcActivityDefinitionBase<CalculateRequest, CalculateResponse>
{
    // AutoGenerateInputs = true のため、
    // CalculateRequest の A, B, Operation プロパティは自動的に入力として生成されます。
    
    // AutoGenerateOutputs = true のため、
    // CalculateResponse の Result プロパティは自動的に出力として生成されます。
    
    // 特定のプロパティだけカスタマイズしたい場合は、
    // [RpcInput] や [RpcOutput] 属性を使って明示的に定義できます。
}
