using System.ComponentModel;

namespace ElsaServer.Messages;

/// <summary>
/// シンプルなテスト用メッセージ
/// </summary>
[DisplayName("テストメッセージ")]
[Description("テスト用のシンプルなメッセージです")]
public class TestMessage
{
    /// <summary>
    /// メッセージ内容
    /// </summary>
    [DisplayName("メッセージ")]
    [Description("テストメッセージの内容")]
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// 優先度
    /// </summary>
    [DisplayName("優先度")]
    [Description("メッセージの優先度")]
    public Priority Priority { get; set; } = Priority.Normal;
}

/// <summary>
/// 優先度
/// </summary>
public enum Priority
{
    /// <summary>低</summary>
    Low,
    /// <summary>通常</summary>
    Normal,
    /// <summary>高</summary>
    High,
    /// <summary>緊急</summary>
    Critical
}
