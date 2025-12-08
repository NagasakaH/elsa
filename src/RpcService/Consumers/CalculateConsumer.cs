using MassTransit;
using NagasakaEventSystem.RpcService.Messages;

namespace NagasakaEventSystem.RpcService.Consumers;

/// <summary>
/// 計算リクエストを処理するコンシューマー
/// </summary>
public class CalculateConsumer : IConsumer<CalculateRequest>
{
    public async Task Consume(ConsumeContext<CalculateRequest> context)
    {
        var request = context.Message;

        // シミュレートされた処理遅延
        await Task.Delay(50);

        try
        {
            var result = request.Operation switch
            {
                CalculateOperation.Add => request.LeftOperand + request.RightOperand,
                CalculateOperation.Subtract => request.LeftOperand - request.RightOperand,
                CalculateOperation.Multiply => request.LeftOperand * request.RightOperand,
                CalculateOperation.Divide => request.RightOperand == 0 
                    ? throw new DivideByZeroException("ゼロで除算することはできません") 
                    : request.LeftOperand / request.RightOperand,
                _ => throw new ArgumentException($"不明な演算子: {request.Operation}")
            };

            await context.RespondAsync(new CalculateResponse
            {
                Result = result,
                Success = true
            });
        }
        catch (Exception ex)
        {
            await context.RespondAsync(new CalculateResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }
}
