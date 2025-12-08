using System.ComponentModel;
using Elsa.ServiceBus.MassTransit.Attributes;
using Elsa.ServiceBus.MassTransit.Builders;
using NagasakaEventSystem.RpcService.Messages;

namespace NagasakaEventSystem.RpcService.Activities;

/// <summary>
/// 通知送信アクティビティ
/// 
/// 方法2: 属性 + プロパティで詳細定義
/// - Enumリスト（Channels）をJSONで入力する例
/// - Dictionary（TemplateVariables）をJSONで入力する例
/// - 複数のEnumプロパティを持つ例
/// </summary>
[RpcActivity(
    DisplayName = "通知送信",
    Description = "メール、プッシュ通知、SMSなど複数チャンネルで通知を送信します。",
    Category = "通知",
    DestinationQueue = "SendNotification",
    TimeoutSeconds = 60,
    AutoGenerateInputs = false,
    AutoGenerateOutputs = false
)]
public class SendNotificationActivity : RpcActivityDefinitionBase<SendNotificationRequest, SendNotificationResponse>
{
    // ========================================
    // 基本設定（入力）
    // ========================================

    [RpcInput(
        DisplayName = "受信者ID",
        Description = "通知を受け取るユーザーのID",
        SourcePath = "RecipientId",
        Order = 1,
        IsRequired = true)]
    public string RecipientId { get; set; } = string.Empty;

    [RpcInput(
        DisplayName = "通知タイプ",
        Description = "通知の種類（Info, Warning, Alert, Promotion, Reminder, System）",
        SourcePath = "Type",
        Order = 2,
        UIHint = "dropdown")]
    public NotificationType Type { get; set; } = NotificationType.Info;

    [RpcInput(
        DisplayName = "件名",
        Description = "通知のタイトル",
        SourcePath = "Subject",
        Order = 3,
        IsRequired = true)]
    public string Subject { get; set; } = string.Empty;

    [RpcInput(
        DisplayName = "本文",
        Description = "通知の本文内容",
        SourcePath = "Body",
        Order = 4,
        UIHint = "multiline",
        IsRequired = true)]
    public string Body { get; set; } = string.Empty;

    // ========================================
    // 送信チャンネル設定
    // ========================================

    [RpcCollectionInput(
        DisplayName = "送信チャンネル",
        Description = "通知を送信するチャンネルのリスト。JSON形式で入力: [\"Email\", \"Push\", \"InApp\"]",
        SourcePath = "Channels",
        Order = 5,
        UseJsonInput = true,
        Example = "[\"Email\", \"Push\"]")]
    public List<NotificationChannel> Channels { get; set; } = new();

    [RpcInput(
        DisplayName = "優先度",
        Description = "通知の優先度（Low, Normal, High, Urgent）",
        SourcePath = "Priority",
        Order = 6,
        UIHint = "dropdown")]
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    // ========================================
    // テンプレート設定（任意）
    // ========================================

    [RpcInput(
        DisplayName = "テンプレートID",
        Description = "使用するテンプレートのID（任意）。指定すると件名・本文がテンプレートから生成されます",
        SourcePath = "TemplateId",
        Order = 10)]
    public string? TemplateId { get; set; }

    [RpcInput(
        DisplayName = "テンプレート変数",
        Description = "テンプレートに埋め込む変数。JSON形式で入力: {\"name\": \"山田太郎\", \"date\": \"2025/1/1\"}",
        SourcePath = "TemplateVariables",
        Order = 11,
        UIHint = "multiline",
        Example = "{\"userName\": \"山田太郎\", \"orderNumber\": \"ORD-001\"}")]
    public Dictionary<string, string> TemplateVariables { get; set; } = new();

    // ========================================
    // スケジュール設定（任意）
    // ========================================

    [RpcInput(
        DisplayName = "予約送信日時",
        Description = "指定した日時に送信（空の場合は即時送信）。ISO 8601形式: 2025-01-01T10:00:00",
        SourcePath = "ScheduledAt",
        Order = 20)]
    public DateTime? ScheduledAt { get; set; }

    // ========================================
    // 結果（出力）
    // ========================================

    [RpcOutput(
        DisplayName = "成功",
        Description = "通知送信が成功したかどうか",
        SourcePath = "Success",
        Order = 1)]
    public bool Success { get; set; }

    [RpcOutput(
        DisplayName = "通知ID",
        Description = "作成された通知のID",
        SourcePath = "NotificationId",
        Order = 2)]
    public string NotificationId { get; set; } = string.Empty;

    [RpcOutput(
        DisplayName = "チャンネル別結果",
        Description = "各チャンネルの送信結果",
        SourcePath = "ChannelResults",
        Order = 3)]
    public Dictionary<NotificationChannel, bool> ChannelResults { get; set; } = new();

    [RpcOutput(
        DisplayName = "予約済み",
        Description = "予約送信としてスケジュールされたかどうか",
        SourcePath = "IsScheduled",
        Order = 4)]
    public bool IsScheduled { get; set; }

    [RpcOutput(
        DisplayName = "エラーメッセージ",
        Description = "エラーが発生した場合のメッセージ",
        SourcePath = "ErrorMessage",
        Order = 10)]
    public string? ErrorMessage { get; set; }
}
