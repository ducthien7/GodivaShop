using GodivaShop.Web.Data;
using GodivaShop.Web.Hubs;
using GodivaShop.Web.Models.Domain;
using GodivaShop.Web.Models.ViewModels;
using GodivaShop.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

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
        if (quantity <= 0)
        {
            _cart.RemoveItem(productId, variantId);
        }
        else
        {
            _cart.UpdateQuantity(productId, variantId, quantity);
        }
        return Json(new { success = true, cartCount = _cart.GetCartCount() });
    }

    [HttpPost]
    public IActionResult Remove(int productId, int? variantId)
    {
        _cart.RemoveItem(productId, variantId);
        return Json(new
        {
            success = true,
            cartCount = _cart.GetCartCount()
        });
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

        // Lưu thông tin checkout vào Session để dùng lại ở ConfirmPayment
        HttpContext.Session.SetString("CheckoutInfo", JsonConvert.SerializeObject(vm));

        // Tính toán thông tin thanh toán
        decimal totalAmount = cartItems.Sum(x => x.Subtotal);
        decimal discountAmount = 0;

        if (!string.IsNullOrEmpty(vm.CouponCode))
        {
            var coupon = _db.Coupons.FirstOrDefault(c => c.Code == vm.CouponCode && c.IsActive);
            if (coupon != null)
            {
                discountAmount = coupon.DiscountType == DiscountType.Percentage
                    ? totalAmount * coupon.DiscountValue / 100
                    : coupon.DiscountValue;
            }
        }

        // Lưu thông tin thanh toán vào ViewBag để hiển thị trên trang xác nhận
        ViewBag.TotalAmount = totalAmount;
        ViewBag.DiscountAmount = discountAmount;
        ViewBag.FinalAmount = totalAmount - discountAmount;
        ViewBag.CartItems = cartItems;
        ViewBag.CheckoutInfo = vm;

        return View("ConfirmPayment");
    }

    // Trang xác nhận thanh toán
    public async Task<IActionResult> ConfirmPayment()
    {
        var cartItems = _cart.GetCart();
        if (!cartItems.Any())
            return RedirectToAction("Index");

        // Lấy thông tin checkout từ Session
        var checkoutInfoJson = HttpContext.Session.GetString("CheckoutInfo");
        if (string.IsNullOrEmpty(checkoutInfoJson))
            return RedirectToAction("Checkout");

        var vm = JsonConvert.DeserializeObject<CheckoutViewModel>(checkoutInfoJson);

        // Tính toán
        decimal totalAmount = cartItems.Sum(x => x.Subtotal);
        decimal discountAmount = 0;

        if (!string.IsNullOrEmpty(vm?.CouponCode))
        {
            var coupon = _db.Coupons.FirstOrDefault(c => c.Code == vm.CouponCode && c.IsActive);
            if (coupon != null)
            {
                discountAmount = coupon.DiscountType == DiscountType.Percentage
                    ? totalAmount * coupon.DiscountValue / 100
                    : coupon.DiscountValue;
            }
        }

        ViewBag.TotalAmount = totalAmount;
        ViewBag.DiscountAmount = discountAmount;
        ViewBag.FinalAmount = totalAmount - discountAmount;
        ViewBag.CartItems = cartItems;
        ViewBag.CheckoutInfo = vm;

        return View();
    }

    // Xác nhận và tạo order
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessPayment()
    {
        try
        {
            var cartItems = _cart.GetCart();
            if (!cartItems.Any())
                return RedirectToAction("Index");

            // Lấy thông tin checkout từ Session
            var checkoutInfoJson = HttpContext.Session.GetString("CheckoutInfo");
            if (string.IsNullOrEmpty(checkoutInfoJson))
                return RedirectToAction("Checkout");

            var vm = JsonConvert.DeserializeObject<CheckoutViewModel>(checkoutInfoJson);
            if (vm == null)
                return RedirectToAction("Checkout");

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

            // Verify order was created
            if (order.Id <= 0)
                return BadRequest("Không thể tạo đơn hàng");

            // Gửi email xác nhận
            try
            {
                await _emailService.SendOrderConfirmationAsync(
                    order.GuestEmail, order.GuestFullName, order.Id, order.TotalAmount - order.DiscountAmount);
            }
            catch
            {
                // Email sending failure should not block order creation
            }

            // SignalR: Thông báo Admin
            try
            {
                await _hubContext.Clients.Group("AdminGroup")
                    .SendAsync("NewOrder", new
                    {
                        orderId = order.Id,
                        customerName = order.GuestFullName,
                        amount = order.TotalAmount - order.DiscountAmount,
                        isGuest = order.UserId == null
                    });
            }
            catch
            {
                // SignalR failure should not block order creation
            }

            _cart.ClearCart();
            HttpContext.Session.Remove("CheckoutInfo");

            return RedirectToAction("PaymentSuccess", new { id = order.Id });
        }
        catch (Exception ex)
        {
            return BadRequest($"Lỗi khi xử lý thanh toán: {ex.Message}");
        }
    }

    public IActionResult PaymentSuccess(int id)
    {
        if (id <= 0)
            return BadRequest("Mã đơn hàng không hợp lệ");

        ViewBag.OrderId = id;

        // Verify order exists in database
        var order = _db.Orders.Find(id);
        if (order == null)
        {
            return BadRequest($"Không tìm thấy đơn hàng #{id}");
        }

        return View();
    }

    // Diagnostic endpoint - helps debug the payment flow
    [HttpGet]
    public IActionResult PaymentDiagnostic()
    {
        var cartItems = _cart.GetCart();
        var checkoutInfo = HttpContext.Session.GetString("CheckoutInfo");

        return Json(new
        {
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            cartItemsCount = cartItems.Count,
            hasCheckoutInfo = !string.IsNullOrEmpty(checkoutInfo),
            sessionId = HttpContext.Session.Id,
            lastOrders = _db.Orders.OrderByDescending(o => o.OrderDate).Take(5).Select(o => new { o.Id, o.GuestFullName, o.OrderDate, o.TotalAmount }).ToList()
        });
    }
}