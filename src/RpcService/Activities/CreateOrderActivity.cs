using System.ComponentModel;
using Elsa.ServiceBus.MassTransit.Attributes;
using Elsa.ServiceBus.MassTransit.Builders;
using NagasakaEventSystem.RpcService.Messages;

namespace NagasakaEventSystem.RpcService.Activities;

/// <summary>
/// 注文作成アクティビティ（方法2: 属性ベース）
/// 
/// 複雑なネストを持つリクエストの例:
/// - Customer（顧客情報）のフラット化
/// - ShippingAddress（配送先住所）のフラット化  
/// - Items（注文明細）をJSONエディタで入力
/// - Metadata（メタデータ）をJSONで入力
/// </summary>
[RpcActivity(
    DisplayName = "注文作成",
    Description = "新しい注文を作成します。顧客情報、配送先、注文明細を指定して注文を登録します。",
    Category = "注文管理",
    DestinationQueue = "CreateOrder",
    TimeoutSeconds = 60,
    AutoGenerateInputs = false,
    AutoGenerateOutputs = false
)]
public class CreateOrderActivity : RpcActivityDefinitionBase<CreateOrderRequest, CreateOrderResponse>
{
    // ========================================
    // 基本情報
    // ========================================

    [RpcInput(
        DisplayName = "注文ID",
        Description = "注文の一意識別子（空の場合は自動生成）",
        SourcePath = "OrderId",
        Order = 1)]
    public string OrderId { get; set; } = string.Empty;

    // ========================================
    // 顧客情報（Customer をフラット化）
    // ========================================

    [RpcInput(
        DisplayName = "顧客ID",
        Description = "注文する顧客のID",
        SourcePath = "Customer.CustomerId",
        Order = 10,
        IsRequired = true)]
    public string CustomerId { get; set; } = string.Empty;

    [RpcInput(
        DisplayName = "顧客名",
        Description = "顧客の氏名",
        SourcePath = "Customer.Name",
        Order = 11,
        IsRequired = true)]
    public string CustomerName { get; set; } = string.Empty;

    [RpcInput(
        DisplayName = "メールアドレス",
        Description = "注文確認メールの送信先",
        SourcePath = "Customer.Email",
        Order = 12,
        IsRequired = true)]
    public string CustomerEmail { get; set; } = string.Empty;

    [RpcInput(
        DisplayName = "電話番号",
        Description = "配送に関する連絡先（任意）",
        SourcePath = "Customer.Phone",
        Order = 13)]
    public string? CustomerPhone { get; set; }

    // ========================================
    // 配送先住所（ShippingAddress をフラット化）
    // ========================================

    [RpcInput(
        DisplayName = "郵便番号",
        Description = "配送先の郵便番号（ハイフンなし）",
        SourcePath = "ShippingAddress.PostalCode",
        Order = 20,
        IsRequired = true,
        Example = "1234567")]
    public string PostalCode { get; set; } = string.Empty;

    [RpcInput(
        DisplayName = "都道府県",
        Description = "配送先の都道府県",
        SourcePath = "ShippingAddress.Prefecture",
        Order = 21,
        IsRequired = true,
        Example = "東京都")]
    public string Prefecture { get; set; } = string.Empty;

    [RpcInput(
        DisplayName = "市区町村",
        Description = "配送先の市区町村",
        SourcePath = "ShippingAddress.City",
        Order = 22,
        IsRequired = true,
        Example = "渋谷区")]
    public string City { get; set; } = string.Empty;

    [RpcInput(
        DisplayName = "番地",
        Description = "配送先の番地",
        SourcePath = "ShippingAddress.Street",
        Order = 23,
        IsRequired = true,
        Example = "1-2-3")]
    public string Street { get; set; } = string.Empty;

    [RpcInput(
        DisplayName = "建物名・部屋番号",
        Description = "マンション名、部屋番号など（任意）",
        SourcePath = "ShippingAddress.Building",
        Order = 24,
        Example = "○○マンション 101号室")]
    public string? Building { get; set; }

    // ========================================
    // 注文明細（コレクションをJSONで入力）
    // ========================================

    [RpcCollectionInput(
        DisplayName = "注文明細",
        Description = "注文する商品のリスト。JSON形式で入力してください。",
        SourcePath = "Items",
        Order = 30,
        UseJsonInput = true,
        Example = "[{\"ProductId\": \"PROD001\", \"ProductName\": \"商品A\", \"Quantity\": 2, \"UnitPrice\": 1000}]")]
    public List<OrderItem> Items { get; set; } = new();

    // ========================================
    // その他のオプション
    // ========================================

    [RpcInput(
        DisplayName = "メタデータ",
        Description = "追加の情報（JSON形式）",
        SourcePath = "Metadata",
        Order = 40,
        UIHint = "multiline",
        Example = "{\"campaign\": \"winter2025\", \"referrer\": \"email\"}")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    [RpcInput(
        DisplayName = "優先配送",
        Description = "優先配送を希望する場合はチェック",
        SourcePath = "PriorityShipping",
        Order = 41,
        UIHint = "checkbox")]
    public bool PriorityShipping { get; set; }

    [RpcInput(
        DisplayName = "備考",
        Description = "注文に関する備考やメモ",
        SourcePath = "Notes",
        Order = 42,
        UIHint = "multiline")]
    public string? Notes { get; set; }

    // ========================================
    // 結果（出力）
    // ========================================

    [RpcOutput(
        DisplayName = "成功",
        Description = "注文作成が成功したかどうか",
        SourcePath = "Success",
        Order = 1)]
    public bool Success { get; set; }

    [RpcOutput(
        DisplayName = "注文ID",
        Description = "作成された注文のID",
        SourcePath = "OrderId",
        Order = 2)]
    public string ResultOrderId { get; set; } = string.Empty;

    [RpcOutput(
        DisplayName = "注文番号",
        Description = "発行された注文番号",
        SourcePath = "OrderNumber",
        Order = 3)]
    public string OrderNumber { get; set; } = string.Empty;

    [RpcOutput(
        DisplayName = "合計金額",
        Description = "注文の合計金額（税込）",
        SourcePath = "TotalAmount",
        Order = 4)]
    public decimal TotalAmount { get; set; }

    [RpcOutput(
        DisplayName = "消費税",
        Description = "消費税額",
        SourcePath = "TaxAmount",
        Order = 5)]
    public decimal TaxAmount { get; set; }

    [RpcOutput(
        DisplayName = "配送予定日",
        Description = "配送予定日",
        SourcePath = "EstimatedDeliveryDate",
        Order = 6)]
    public DateTime? EstimatedDeliveryDate { get; set; }

    [RpcOutput(
        DisplayName = "エラーメッセージ",
        Description = "エラーが発生した場合のメッセージ",
        SourcePath = "ErrorMessage",
        Order = 10)]
    public string? ErrorMessage { get; set; }

    [RpcOutput(
        DisplayName = "検証エラー",
        Description = "入力値の検証エラーリスト",
        SourcePath = "ValidationErrors",
        Order = 11)]
    public List<ValidationError>? ValidationErrors { get; set; }
}
