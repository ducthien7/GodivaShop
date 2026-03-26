using System.ComponentModel.DataAnnotations;

namespace GodivaShop.Web.Models.ViewModels;

public class ProductCreateViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá")]
    [Range(1000, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
    public decimal BasePrice { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn danh mục")]
    public int CategoryId { get; set; }

    public bool IsBestSeller { get; set; }
    public bool IsActive { get; set; } = true;

    // Ảnh upload
    public List<IFormFile>? Images { get; set; }
}