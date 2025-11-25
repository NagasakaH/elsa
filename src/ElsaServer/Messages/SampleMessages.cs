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
}
