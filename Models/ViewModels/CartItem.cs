namespace GodivaShop.Web.Models.ViewModels;

public class CartItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public int? VariantId { get; set; }
    public string? VariantName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? GiftMessage { get; set; }
    public decimal Subtotal => UnitPrice * Quantity;
}