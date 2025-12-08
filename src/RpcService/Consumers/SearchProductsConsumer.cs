using MassTransit;
using NagasakaEventSystem.RpcService.Messages;

namespace NagasakaEventSystem.RpcService.Consumers;

/// <summary>
/// 商品検索リクエストを処理するコンシューマー
/// </summary>
public class SearchProductsConsumer : IConsumer<SearchProductsRequest>
{
    // モック商品データ
    private static readonly List<ProductSummary> MockProducts = new()
    {
        new ProductSummary { ProductId = "PRD001", Name = "ワイヤレスマウス", Description = "高精度センサー搭載", Price = 3980, InStock = true, StockQuantity = 50 },
        new ProductSummary { ProductId = "PRD002", Name = "メカニカルキーボード", Description = "Cherry MX青軸", Price = 12800, InStock = true, StockQuantity = 20 },
        new ProductSummary { ProductId = "PRD003", Name = "27インチモニター", Description = "4K IPS パネル", Price = 45000, InStock = true, StockQuantity = 10 },
        new ProductSummary { ProductId = "PRD004", Name = "USBハブ", Description = "7ポート USB3.0", Price = 2480, InStock = false, StockQuantity = 0 },
        new ProductSummary { ProductId = "PRD005", Name = "Webカメラ", Description = "1080p フルHD", Price = 5980, InStock = true, StockQuantity = 30 },
        new ProductSummary { ProductId = "PRD006", Name = "ヘッドセット", Description = "ノイズキャンセリング対応", Price = 8900, InStock = true, StockQuantity = 15 },
        new ProductSummary { ProductId = "PRD007", Name = "外付けSSD 1TB", Description = "NVMe 高速転送", Price = 15800, InStock = true, StockQuantity = 25 },
        new ProductSummary { ProductId = "PRD008", Name = "モニターアーム", Description = "デュアル対応", Price = 6800, InStock = false, StockQuantity = 0 },
        new ProductSummary { ProductId = "PRD009", Name = "デスクマット", Description = "大型 800x400mm", Price = 2980, InStock = true, StockQuantity = 100 },
        new ProductSummary { ProductId = "PRD010", Name = "ケーブル収納ボックス", Description = "配線すっきり", Price = 1980, InStock = true, StockQuantity = 80 },
    };

    public async Task Consume(ConsumeContext<SearchProductsRequest> context)
    {
        var request = context.Message;
        Console.WriteLine($"[SearchProducts] リクエスト受信: Keyword={request.Keyword}, Category={request.CategoryId}");

        // シミュレートされた処理遅延
        await Task.Delay(100);

        var results = MockProducts.AsEnumerable();

        // キーワードフィルター
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.ToLower();
            results = results.Where(p => 
                p.Name.ToLower().Contains(keyword) || 
                (p.Description?.ToLower().Contains(keyword) ?? false));
        }

        // 価格フィルター
        if (request.MinPrice.HasValue)
        {
            results = results.Where(p => p.Price >= request.MinPrice.Value);
        }
        if (request.MaxPrice.HasValue)
        {
            results = results.Where(p => p.Price <= request.MaxPrice.Value);
        }

        // 在庫フィルター
        if (request.InStockOnly)
        {
            results = results.Where(p => p.InStock);
        }

        // ソート
        results = request.SortOrder switch
        {
            ProductSortOrder.PriceAsc => results.OrderBy(p => p.Price),
            ProductSortOrder.PriceDesc => results.OrderByDescending(p => p.Price),
            ProductSortOrder.Newest => results.OrderByDescending(p => p.ProductId),
            ProductSortOrder.BestSelling => results.OrderBy(p => p.StockQuantity),
            _ => results
        };

        var allResults = results.ToList();
        var totalCount = allResults.Count;
        var pageSize = request.PageSize > 0 ? request.PageSize : 20;
        var page = request.Page > 0 ? request.Page : 1;
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var pagedResults = allResults
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        Console.WriteLine($"[SearchProducts] レスポンス送信: {pagedResults.Count}件 / 総{totalCount}件");

        await context.RespondAsync(new SearchProductsResponse
        {
            Products = pagedResults,
            TotalCount = totalCount,
            TotalPages = totalPages,
            CurrentPage = page
        });
    }
}
