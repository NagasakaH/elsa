using System.ComponentModel;

namespace NagasakaEventSystem.RpcService.Messages;

/// <summary>
/// 通知送信リクエスト
/// </summary>
public class SendNotificationRequest
{
    [DisplayName("受信者ID")]
    [Description("通知を受け取るユーザーのID")]
    public string RecipientId { get; set; } = string.Empty;

    [DisplayName("通知タイプ")]
    [Description("通知の種類")]
    public NotificationType Type { get; set; }

    [DisplayName("件名")]
    [Description("通知のタイトル")]
    public string Subject { get; set; } = string.Empty;

    [DisplayName("本文")]
    [Description("通知の本文")]
    public string Body { get; set; } = string.Empty;

    [DisplayName("送信チャンネル")]
    [Description("通知を送信するチャンネル")]
    public List<NotificationChannel> Channels { get; set; } = new();

    [DisplayName("テンプレートID")]
    [Description("使用するテンプレートのID（任意）")]
    public string? TemplateId { get; set; }

    [DisplayName("テンプレート変数")]
    [Description("テンプレートに埋め込む変数")]
    public Dictionary<string, string> TemplateVariables { get; set; } = new();

    [DisplayName("予約送信日時")]
    [Description("指定した日時に送信（空の場合は即時送信）")]
    public DateTime? ScheduledAt { get; set; }

    [DisplayName("優先度")]
    [Description("通知の優先度")]
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
}

/// <summary>
/// 通知タイプ
/// </summary>
public enum NotificationType
{
    Info,
    Warning,
    Alert,
    Promotion,
    Reminder,
    System
}

/// <summary>
/// 通知チャンネル
/// </summary>
public enum NotificationChannel
{
    Email,
    Push,
    Sms,
    InApp,
    Slack,
    Teams
}

/// <summary>
/// 通知優先度
/// </summary>
public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Urgent
}

/// <summary>
/// 通知送信レスポンス
/// </summary>
public class SendNotificationResponse
{
    [DisplayName("成功")]
    public bool Success { get; set; }

    [DisplayName("通知ID")]
    public string NotificationId { get; set; } = string.Empty;

    [DisplayName("送信状態")]
    public Dictionary<NotificationChannel, bool> ChannelResults { get; set; } = new();

    [DisplayName("エラーメッセージ")]
    public string? ErrorMessage { get; set; }

    [DisplayName("予約済み")]
    public bool IsScheduled { get; set; }
}
