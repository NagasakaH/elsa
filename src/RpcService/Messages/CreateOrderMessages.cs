using System.ComponentModel;

namespace NagasakaEventSystem.RpcService.Messages;

/// <summary>
/// 注文作成リクエスト - ネストしたオブジェクトとコレクションを含む複雑な例
/// </summary>
public class CreateOrderRequest
{
    [DisplayName("注文ID")]
    [Description("注文の一意識別子")]
    public string OrderId { get; set; } = string.Empty;

    [DisplayName("顧客情報")]
    [Description("注文する顧客の情報")]
    public CustomerInfo Customer { get; set; } = new();

    [DisplayName("注文明細")]
    [Description("注文に含まれる商品のリスト")]
    public List<OrderItem> Items { get; set; } = new();

    [DisplayName("配送先住所")]
    [Description("商品の配送先住所")]
    public Address ShippingAddress { get; set; } = new();

    [DisplayName("メタデータ")]
    [Description("追加のメタ情報")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    [DisplayName("優先配送")]
    [Description("優先配送を希望するかどうか")]
    public bool PriorityShipping { get; set; }

    [DisplayName("備考")]
    [Description("注文に関する備考")]
    public string? Notes { get; set; }
}

/// <summary>
/// 顧客情報
/// </summary>
public class CustomerInfo
{
    [DisplayName("顧客ID")]
    public string CustomerId { get; set; } = string.Empty;

    [DisplayName("顧客名")]
    public string Name { get; set; } = string.Empty;

    [DisplayName("メールアドレス")]
    public string Email { get; set; } = string.Empty;

    [DisplayName("電話番号")]
    public string? Phone { get; set; }
}

/// <summary>
/// 注文明細アイテム
/// </summary>
public class OrderItem
{
    [DisplayName("商品ID")]
    public string ProductId { get; set; } = string.Empty;

    [DisplayName("商品名")]
    public string ProductName { get; set; } = string.Empty;

    [DisplayName("数量")]
    public int Quantity { get; set; } = 1;

    [DisplayName("単価")]
    public decimal UnitPrice { get; set; }

    [DisplayName("オプション")]
    public List<string>? Options { get; set; }
}

/// <summary>
/// 住所情報
/// </summary>
public class Address
{
    [DisplayName("郵便番号")]
    public string PostalCode { get; set; } = string.Empty;

    [DisplayName("都道府県")]
    public string Prefecture { get; set; } = string.Empty;

    [DisplayName("市区町村")]
    public string City { get; set; } = string.Empty;

    [DisplayName("番地")]
    public string Street { get; set; } = string.Empty;

    [DisplayName("建物名・部屋番号")]
    public string? Building { get; set; }
}

/// <summary>
/// 注文作成レスポンス
/// </summary>
public class CreateOrderResponse
{
    [DisplayName("成功")]
    public bool Success { get; set; }

    [DisplayName("注文ID")]
    public string OrderId { get; set; } = string.Empty;

    [DisplayName("注文番号")]
    public string OrderNumber { get; set; } = string.Empty;

    [DisplayName("合計金額")]
    public decimal TotalAmount { get; set; }

    [DisplayName("消費税")]
    public decimal TaxAmount { get; set; }

    [DisplayName("配送予定日")]
    public DateTime? EstimatedDeliveryDate { get; set; }

    [DisplayName("エラーメッセージ")]
    public string? ErrorMessage { get; set; }

    [DisplayName("検証エラー")]
    public List<ValidationError>? ValidationErrors { get; set; }
}

/// <summary>
/// 検証エラー
/// </summary>
public class ValidationError
{
    [DisplayName("フィールド")]
    public string Field { get; set; } = string.Empty;

    [DisplayName("メッセージ")]
    public string Message { get; set; } = string.Empty;
}
