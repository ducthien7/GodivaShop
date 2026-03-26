using GodivaShop.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace GodivaShop.Web.Services;

public class BestSellerService
{
    private readonly ApplicationDbContext _db;

    public BestSellerService(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Cập nhật flag IsBestSeller dựa trên số lượng bán
    /// Sản phẩm nằm trong top 20% bán được nhiều nhất sẽ được đánh dấu là BestSeller
    /// </summary>
    public async Task UpdateBestSellersAsync()
    {
        // Lấy tất cả sản phẩm với tổng số lượng bán
        var productSales = await _db.Products
            .Include(p => p.OrderItems)
            .Select(p => new
            {
                ProductId = p.Id,
                TotalQuantity = p.OrderItems.Sum(oi => oi.Quantity)
            })
            .ToListAsync();

        if (!productSales.Any()) return;

        // Tính số lượng bán trung bình
        var totalSales = productSales.Sum(ps => ps.TotalQuantity);
        var avgSales = productSales.Average(ps => ps.TotalQuantity);
        var topThreshold = avgSales * 1.5; // Sản phẩm bán > 150% trung bình

        // Cập nhật flag
        var productsToUpdate = await _db.Products.ToListAsync();
        foreach (var product in productsToUpdate)
        {
            var sales = productSales.FirstOrDefault(ps => ps.ProductId == product.Id);
            var quantity = sales?.TotalQuantity ?? 0;
            product.IsBestSeller = quantity > 0 && quantity >= topThreshold;
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Lấy danh sách sản phẩm bán chạy nhất (theo số lượng bán)
    /// </summary>
    public async Task<List<(int ProductId, int TotalQuantity)>> GetTopSellingProductsAsync(int limit = 12)
    {
        var topSellers = await _db.OrderItems
            .GroupBy(oi => oi.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                TotalQuantity = g.Sum(oi => oi.Quantity)
            })
            .OrderByDescending(x => x.TotalQuantity)
            .Take(limit)
            .Select(x => new ValueTuple<int, int>(x.ProductId, x.TotalQuantity))
            .ToListAsync();

        return topSellers;
    }

    /// <summary>
    /// Lấy số lượng bán của một sản phẩm
    /// </summary>
    public async Task<int> GetProductSalesCountAsync(int productId)
    {
        return await _db.OrderItems
            .Where(oi => oi.ProductId == productId)
            .SumAsync(oi => oi.Quantity);
    }
}
