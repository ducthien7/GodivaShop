using System.ComponentModel.DataAnnotations;

namespace GodivaShop.Web.Models.ViewModels;

public class CheckoutViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
    public string ShippingAddress { get; set; } = string.Empty;

    public string? CouponCode { get; set; }
    public string? Note { get; set; }

    public List<CartItem> CartItems { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount => TotalAmount - DiscountAmount;
    public string PaymentMethod { get; set; } = "COD";
}