using System.ComponentModel;
using Elsa.ServiceBus.MassTransit.Attributes;
using Elsa.ServiceBus.MassTransit.Builders;
using NagasakaEventSystem.RpcService.Messages;

namespace NagasakaEventSystem.RpcService.Activities;

/// <summary>
/// 商品検索アクティビティ
/// 
/// 方法2: 属性 + プロパティで詳細定義
/// - ネストしていないシンプルな入力を持つ例
/// - Enumはドロップダウンで表示
/// - オプショナルな入力とデフォルト値の設定
/// </summary>
[RpcActivity(
    DisplayName = "商品検索",
    Description = "キーワード、カテゴリ、価格範囲などの条件で商品を検索します。",
    Category = "商品管理",
    TimeoutSeconds = 30,
    AutoGenerateInputs = false,
    AutoGenerateOutputs = false
)]
public class SearchProductsActivity : RpcActivityDefinitionBase<SearchProductsRequest, SearchProductsResponse>
{
    // ========================================
    // 検索条件（入力）
    // ========================================

    [RpcInput(
        DisplayName = "検索キーワード",
        Description = "商品名や説明に含まれるキーワード（任意）",
        SourcePath = "Keyword",
        Order = 1)]
    public string? Keyword { get; set; }

    [RpcInput(
        DisplayName = "カテゴリID",
        Description = "絞り込むカテゴリのID（任意）",
        SourcePath = "CategoryId",
        Order = 2)]
    public string? CategoryId { get; set; }

    [RpcInput(
        DisplayName = "最低価格",
        Description = "価格範囲の下限（任意）",
        SourcePath = "MinPrice",
        Order = 3)]
    public decimal? MinPrice { get; set; }

    [RpcInput(
        DisplayName = "最高価格",
        Description = "価格範囲の上限（任意）",
        SourcePath = "MaxPrice",
        Order = 4)]
    public decimal? MaxPrice { get; set; }

    [RpcInput(
        DisplayName = "在庫ありのみ",
        Description = "在庫がある商品のみを検索する場合はチェック",
        SourcePath = "InStockOnly",
        Order = 5,
        UIHint = "checkbox",
        DefaultValue = true)]
    public bool InStockOnly { get; set; } = true;

    [RpcInput(
        DisplayName = "ソート順",
        Description = "検索結果の並び順を選択",
        SourcePath = "SortOrder",
        Order = 6,
        UIHint = "dropdown")]
    public ProductSortOrder SortOrder { get; set; } = ProductSortOrder.Relevance;

    [RpcInput(
        DisplayName = "ページ番号",
        Description = "取得するページ番号（1から開始）",
        SourcePath = "Page",
        Order = 7,
        DefaultValue = 1)]
    public int Page { get; set; } = 1;

    [RpcInput(
        DisplayName = "ページサイズ",
        Description = "1ページあたりの件数",
        SourcePath = "PageSize",
        Order = 8,
        DefaultValue = 20)]
    public int PageSize { get; set; } = 20;

    // ========================================
    // 検索結果（出力）
    // ========================================

    [RpcOutput(
        DisplayName = "商品リスト",
        Description = "検索にヒットした商品のリスト",
        SourcePath = "Products",
        Order = 1)]
    public List<ProductSummary> Products { get; set; } = new();

    [RpcOutput(
        DisplayName = "総件数",
        Description = "検索条件に一致した商品の総数",
        SourcePath = "TotalCount",
        Order = 2)]
    public int TotalCount { get; set; }

    [RpcOutput(
        DisplayName = "総ページ数",
        Description = "検索結果の総ページ数",
        SourcePath = "TotalPages",
        Order = 3)]
    public int TotalPages { get; set; }

    [RpcOutput(
        DisplayName = "現在のページ",
        Description = "現在表示しているページ番号",
        SourcePath = "CurrentPage",
        Order = 4)]
    public int CurrentPage { get; set; }
}
