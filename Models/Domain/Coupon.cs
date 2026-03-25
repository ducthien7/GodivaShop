namespace GodivaShop.Web.Models.Domain;

public enum DiscountType { Percentage, FixedAmount }

public class Coupon
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public int? MaxUsageCount { get; set; }
    public int UsedCount { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
}