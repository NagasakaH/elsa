using System.ComponentModel;

namespace NagasakaEventSystem.RpcService.Messages;

/// <summary>
/// 商品検索リクエスト
/// </summary>
public class SearchProductsRequest
{
    [DisplayName("検索キーワード")]
    [Description("商品名や説明に含まれるキーワード")]
    public string? Keyword { get; set; }

    [DisplayName("カテゴリID")]
    [Description("商品カテゴリのID")]
    public string? CategoryId { get; set; }

    [DisplayName("最低価格")]
    [Description("検索する価格範囲の下限")]
    public decimal? MinPrice { get; set; }

    [DisplayName("最高価格")]
    [Description("検索する価格範囲の上限")]
    public decimal? MaxPrice { get; set; }

    [DisplayName("在庫あり")]
    [Description("在庫がある商品のみを検索")]
    public bool InStockOnly { get; set; } = true;

    [DisplayName("ソート順")]
    [Description("検索結果の並び順")]
    public ProductSortOrder SortOrder { get; set; } = ProductSortOrder.Relevance;

    [DisplayName("ページ番号")]
    public int Page { get; set; } = 1;

    [DisplayName("ページサイズ")]
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 商品ソート順
/// </summary>
public enum ProductSortOrder
{
    Relevance,
    PriceAsc,
    PriceDesc,
    Newest,
    BestSelling
}

/// <summary>
/// 商品検索レスポンス
/// </summary>
public class SearchProductsResponse
{
    [DisplayName("商品リスト")]
    public List<ProductSummary> Products { get; set; } = new();

    [DisplayName("総件数")]
    public int TotalCount { get; set; }

    [DisplayName("ページ数")]
    public int TotalPages { get; set; }

    [DisplayName("現在のページ")]
    public int CurrentPage { get; set; }
}

/// <summary>
/// 商品概要
/// </summary>
public class ProductSummary
{
    public string ProductId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool InStock { get; set; }
    public int StockQuantity { get; set; }
}
