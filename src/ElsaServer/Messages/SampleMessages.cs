using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ElsaServer.Messages;

/// <summary>
/// 注文が作成されたときに発行されるメッセージ
/// </summary>
[DisplayName("注文作成")]
[Description("新しい注文が作成されたときに発行されるメッセージです")]
public class OrderCreated
{
    /// <summary>
    /// 注文ID
    /// </summary>
    [DisplayName("注文ID")]
    [Description("一意の注文識別子")]
    [Required]
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// 顧客ID
    /// </summary>
    [DisplayName("顧客ID")]
    [Description("注文した顧客の識別子")]
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// 注文金額
    /// </summary>
    [DisplayName("注文金額")]
    [Description("注文の合計金額")]
    public decimal Amount { get; set; }

    /// <summary>
    /// 注文日時
    /// </summary>
    [DisplayName("注文日時")]
    [Description("注文が作成された日時")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 注文が承認されたときに発行されるメッセージ
/// </summary>
[DisplayName("注文承認")]
[Description("注文が承認されたときに発行されるメッセージです")]
public class OrderApproved
{
    /// <summary>
    /// 注文ID
    /// </summary>
    [DisplayName("注文ID")]
    [Description("承認された注文の識別子")]
    [Required]
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// 承認者ID
    /// </summary>
    [DisplayName("承認者ID")]
    [Description("注文を承認したユーザーの識別子")]
    public string ApprovedBy { get; set; } = string.Empty;

    /// <summary>
    /// 承認日時
    /// </summary>
    [DisplayName("承認日時")]
    [Description("注文が承認された日時")]
    public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 承認コメント
    /// </summary>
    [DisplayName("承認コメント")]
    [Description("承認時のコメント（任意）")]
    public string? Comments { get; set; }
}

/// <summary>
/// 注文が拒否されたときに発行されるメッセージ
/// </summary>
[DisplayName("注文拒否")]
[Description("注文が拒否されたときに発行されるメッセージです")]
public class OrderRejected
{
    /// <summary>
    /// 注文ID
    /// </summary>
    [DisplayName("注文ID")]
    [Description("拒否された注文の識別子")]
    [Required]
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// 拒否理由
    /// </summary>
    [DisplayName("拒否理由")]
    [Description("注文が拒否された理由")]
    [Required]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 拒否者ID
    /// </summary>
    [DisplayName("拒否者ID")]
    [Description("注文を拒否したユーザーの識別子")]
    public string RejectedBy { get; set; } = string.Empty;

    /// <summary>
    /// 拒否日時
    /// </summary>
    [DisplayName("拒否日時")]
    [Description("注文が拒否された日時")]
    public DateTime RejectedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// ユーザー登録が完了したときに発行されるメッセージ
/// </summary>
[DisplayName("ユーザー登録完了")]
[Description("新しいユーザーの登録が完了したときに発行されるメッセージです")]
public class UserRegistered
{
    /// <summary>
    /// ユーザーID
    /// </summary>
    [DisplayName("ユーザーID")]
    [Description("登録されたユーザーの識別子")]
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// メールアドレス
    /// </summary>
    [DisplayName("メールアドレス")]
    [Description("ユーザーのメールアドレス")]
    [Required]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// ユーザー名
    /// </summary>
    [DisplayName("ユーザー名")]
    [Description("ユーザーの表示名")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 登録日時
    /// </summary>
    [DisplayName("登録日時")]
    [Description("ユーザーが登録された日時")]
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 通知を送信するためのメッセージ
/// </summary>
[DisplayName("通知送信")]
[Description("ユーザーに通知を送信するためのメッセージです")]
public class SendNotification
{
    /// <summary>
    /// 通知先ユーザーID
    /// </summary>
    [DisplayName("通知先ユーザーID")]
    [Description("通知を受け取るユーザーの識別子")]
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 通知タイトル
    /// </summary>
    [DisplayName("通知タイトル")]
    [Description("通知のタイトル")]
    [Required]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 通知本文
    /// </summary>
    [DisplayName("通知本文")]
    [Description("通知の本文メッセージ")]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// 通知タイプ
    /// </summary>
    [DisplayName("通知タイプ")]
    [Description("通知の種類")]
    public NotificationType Type { get; set; } = NotificationType.Info;

    /// <summary>
    /// 優先度
    /// </summary>
    [DisplayName("優先度")]
    [Description("通知の優先度（true=高優先度）")]
    public bool IsHighPriority { get; set; } = false;
}

/// <summary>
/// 通知タイプの列挙型
/// </summary>
public enum NotificationType
{
    /// <summary>情報</summary>
    Info,
    /// <summary>成功</summary>
    Success,
    /// <summary>警告</summary>
    Warning,
    /// <summary>エラー</summary>
    Error
}
