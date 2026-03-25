using GodivaShop.Web.Data;
using GodivaShop.Web.Hubs;
using GodivaShop.Web.Models.Domain;
using GodivaShop.Web.Models.ViewModels;
using GodivaShop.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace GodivaShop.Web.Controllers;

public class CartController : Controller
{
    private readonly CartService _cart;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly EmailService _emailService;
    private readonly IHubContext<OrderHub> _hubContext;

    public CartController(CartService cart, ApplicationDbContext db,
        UserManager<ApplicationUser> userManager, EmailService emailService,
        IHubContext<OrderHub> hubContext)
    {
        _cart = cart;
        _db = db;
        _userManager = userManager;
        _emailService = emailService;
        _hubContext = hubContext;
    }

    public IActionResult Index()
    {
        ViewBag.CartCount = _cart.GetCartCount();
        return View(_cart.GetCart());
    }

    [HttpPost]
    public IActionResult UpdateQuantity(int productId, int? variantId, int quantity)
    {
        _cart.UpdateQuantity(productId, variantId, quantity);
        return Json(new { success = true, cartCount = _cart.GetCartCount() });
    }

    [HttpPost]
    public IActionResult Remove(int productId, int? variantId)
    {
        _cart.RemoveItem(productId, variantId);
        return Json(new { success = true, cartCount = _cart.GetCartCount() });
    }

    // Trang thanh toán
    public async Task<IActionResult> Checkout()
    {
        ViewBag.CartCount = _cart.GetCartCount();
        var vm = new CheckoutViewModel { CartItems = _cart.GetCart() };
        vm.TotalAmount = vm.CartItems.Sum(x => x.Subtotal);

        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                vm.FullName = user.FullName;
                vm.Email = user.Email!;
                vm.Phone = user.Phone ?? "";
                vm.ShippingAddress = user.Address ?? "";
            }
        }
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> ApplyCoupon(string code, decimal total)
    {
        var coupon = _db.Coupons.FirstOrDefault(c =>
            c.Code == code && c.IsActive &&
            (c.ExpiryDate == null || c.ExpiryDate >= DateTime.Now) &&
            (c.MaxUsageCount == null || c.UsedCount < c.MaxUsageCount) &&
            (c.MinOrderAmount == null || total >= c.MinOrderAmount));

        if (coupon == null)
            return Json(new { success = false, message = "Mã giảm giá không hợp lệ" });

        var discount = coupon.DiscountType == DiscountType.Percentage
            ? total * coupon.DiscountValue / 100
            : coupon.DiscountValue;

        return Json(new { success = true, discount, message = $"Áp dụng thành công! Giảm {discount:N0}đ" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CheckoutViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("Checkout", vm);

        var cartItems = _cart.GetCart();
        if (!cartItems.Any())
            return RedirectToAction("Index");

        // Tạo Order
        var order = new Order
        {
            GuestFullName = vm.FullName,
            GuestEmail = vm.Email,
            GuestPhone = vm.Phone,
            ShippingAddress = vm.ShippingAddress,
            Note = vm.Note,
            CouponCode = vm.CouponCode,
            TotalAmount = cartItems.Sum(x => x.Subtotal),
        };

        // Áp dụng coupon
        if (!string.IsNullOrEmpty(vm.CouponCode))
        {
            var coupon = _db.Coupons.FirstOrDefault(c => c.Code == vm.CouponCode && c.IsActive);
            if (coupon != null)
            {
                order.DiscountAmount = coupon.DiscountType == DiscountType.Percentage
                    ? order.TotalAmount * coupon.DiscountValue / 100
                    : coupon.DiscountValue;
                coupon.UsedCount++;
            }
        }

        // Gán UserId nếu đã đăng nhập
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            order.UserId = user?.Id;
        }

        // OrderItems
        foreach (var item in cartItems)
        {
            order.OrderItems.Add(new OrderItem
            {
                ProductId = item.ProductId,
                ProductVariantId = item.VariantId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                GiftMessage = item.GiftMessage
            });
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // Gửi email xác nhận
        await _emailService.SendOrderConfirmationAsync(
            order.GuestEmail, order.GuestFullName, order.Id, order.TotalAmount - order.DiscountAmount);

        // SignalR: Thông báo Admin
        await _hubContext.Clients.Group("AdminGroup")
            .SendAsync("NewOrder", new
            {
                orderId = order.Id,
                customerName = order.GuestFullName,
                amount = order.TotalAmount - order.DiscountAmount,
                isGuest = order.UserId == null
            });

        _cart.ClearCart();
        return RedirectToAction("OrderSuccess", new { id = order.Id });
    }

    public IActionResult OrderSuccess(int id)
    {
        ViewBag.OrderId = id;
        return View();
    }
}