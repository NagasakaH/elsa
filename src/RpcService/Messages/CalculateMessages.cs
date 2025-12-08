using System.ComponentModel;

namespace NagasakaEventSystem.RpcService.Messages;

/// <summary>
/// 計算リクエスト
/// </summary>
[DisplayName("計算リクエスト")]
[Description("四則演算を実行するリクエストです")]
public record CalculateRequest
{
    /// <summary>
    /// 左辺の値
    /// </summary>
    [DisplayName("左辺")]
    [Description("計算の左辺の値")]
    public double LeftOperand { get; init; }

    /// <summary>
    /// 右辺の値
    /// </summary>
    [DisplayName("右辺")]
    [Description("計算の右辺の値")]
    public double RightOperand { get; init; }

    /// <summary>
    /// 演算子
    /// </summary>
    [DisplayName("演算子")]
    [Description("実行する演算（Add, Subtract, Multiply, Divide）")]
    public CalculateOperation Operation { get; init; } = CalculateOperation.Add;
}

/// <summary>
/// 計算演算の種類
/// </summary>
public enum CalculateOperation
{
    /// <summary>加算</summary>
    [Description("加算")]
    Add,
    /// <summary>減算</summary>
    [Description("減算")]
    Subtract,
    /// <summary>乗算</summary>
    [Description("乗算")]
    Multiply,
    /// <summary>除算</summary>
    [Description("除算")]
    Divide
}

/// <summary>
/// 計算レスポンス
/// </summary>
[DisplayName("計算レスポンス")]
[Description("計算リクエストに対するレスポンスです")]
public record CalculateResponse
{
    /// <summary>
    /// 計算結果
    /// </summary>
    [DisplayName("結果")]
    [Description("計算の結果")]
    public double Result { get; init; }

    /// <summary>
    /// 成功フラグ
    /// </summary>
    [DisplayName("成功")]
    [Description("計算が成功したかどうか")]
    public bool Success { get; init; }

    /// <summary>
    /// エラーメッセージ
    /// </summary>
    [DisplayName("エラーメッセージ")]
    [Description("エラーが発生した場合のメッセージ")]
    public string? ErrorMessage { get; init; }
}
