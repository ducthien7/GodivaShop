namespace GodivaShop.Web.Models.Domain;

public class ProductVariant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // "8 viên", "15 viên"
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
}