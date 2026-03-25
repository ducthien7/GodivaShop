namespace GodivaShop.Web.Models.Domain;

public enum OrderStatus
{
    Pending,
    Confirmed,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}

public class Order
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? CouponCode { get; set; }
    public string? Note { get; set; }

    // ===== GUEST CHECKOUT (nullable) =====
    public string? UserId { get; set; }           // NULL = Guest
    public ApplicationUser? User { get; set; }

    // Thông tin Guest (hoặc override Member)
    public string GuestFullName { get; set; } = string.Empty;
    public string GuestEmail { get; set; } = string.Empty;
    public string GuestPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}