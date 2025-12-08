using MassTransit;
using NagasakaEventSystem.RpcService.Messages;

namespace NagasakaEventSystem.RpcService.Consumers;

/// <summary>
/// ユーザー情報取得リクエストを処理するコンシューマー
/// </summary>
public class GetUserConsumer : IConsumer<GetUserRequest>
{
    // シンプルなモックユーザーデータ
    private static readonly Dictionary<string, (string Name, string Email)> MockUsers = new()
    {
        { "user001", ("山田 太郎", "taro.yamada@example.com") },
        { "user002", ("佐藤 花子", "hanako.sato@example.com") },
        { "user003", ("鈴木 一郎", "ichiro.suzuki@example.com") },
        { "admin", ("管理者", "admin@example.com") }
    };

    public async Task Consume(ConsumeContext<GetUserRequest> context)
    {
        var request = context.Message;

        // シミュレートされた処理遅延
        await Task.Delay(100);

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            await context.RespondAsync(new GetUserResponse
            {
                Success = false,
                ErrorMessage = "ユーザーIDが指定されていません"
            });
            return;
        }

        if (MockUsers.TryGetValue(request.UserId, out var userData))
        {
            await context.RespondAsync(new GetUserResponse
            {
                UserId = request.UserId,
                Name = userData.Name,
                Email = userData.Email,
                Success = true
            });
        }
        else
        {
            await context.RespondAsync(new GetUserResponse
            {
                UserId = request.UserId,
                Success = false,
                ErrorMessage = $"ユーザーID '{request.UserId}' が見つかりません"
            });
        }
    }
}
