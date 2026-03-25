namespace GodivaShop.Web.Models.Domain;

public class OrderItem
{
    public int Id { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? GiftMessage { get; set; } // Lời chúc quà tặng

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
}