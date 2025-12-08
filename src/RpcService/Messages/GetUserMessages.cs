using System.ComponentModel;

namespace NagasakaEventSystem.RpcService.Messages;

/// <summary>
/// ユーザー情報取得リクエスト
/// </summary>
[DisplayName("ユーザー情報取得リクエスト")]
[Description("指定されたユーザーIDに基づいてユーザー情報を取得するリクエストです")]
public record GetUserRequest
{
    /// <summary>
    /// ユーザーID
    /// </summary>
    [DisplayName("ユーザーID")]
    [Description("取得対象のユーザーID")]
    public string UserId { get; init; } = string.Empty;
}

/// <summary>
/// ユーザー情報取得レスポンス
/// </summary>
[DisplayName("ユーザー情報取得レスポンス")]
[Description("ユーザー情報取得リクエストに対するレスポンスです")]
public record GetUserResponse
{
    /// <summary>
    /// ユーザーID
    /// </summary>
    [DisplayName("ユーザーID")]
    [Description("取得したユーザーのID")]
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// ユーザー名
    /// </summary>
    [DisplayName("ユーザー名")]
    [Description("取得したユーザーの名前")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// メールアドレス
    /// </summary>
    [DisplayName("メールアドレス")]
    [Description("取得したユーザーのメールアドレス")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// 成功フラグ
    /// </summary>
    [DisplayName("成功")]
    [Description("リクエストが成功したかどうか")]
    public bool Success { get; init; }

    /// <summary>
    /// エラーメッセージ
    /// </summary>
    [DisplayName("エラーメッセージ")]
    [Description("エラーが発生した場合のメッセージ")]
    public string? ErrorMessage { get; init; }
}
