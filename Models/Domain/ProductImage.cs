namespace GodivaShop.Web.Models.Domain;

public class ProductImage
{
    public int Id { get; set; }
    public string ImagePath { get; set; } = string.Empty; // ~/images/products/xxx.jpg
    public bool IsMain { get; set; }
    public int DisplayOrder { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
}