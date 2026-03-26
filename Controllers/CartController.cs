using GodivaShop.Web.Data;
using GodivaShop.Web.Hubs;
using GodivaShop.Web.Models.Domain;
using GodivaShop.Web.Models.ViewModels;
using GodivaShop.Web.Services;
using GodivaShop.Web.Helpers; // Đảm bảo có dòng này để dùng VnPayLibrary
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
    private readonly IConfiguration _configuration; // Thêm config để đọc appsettings

    public CartController(CartService cart, ApplicationDbContext db,
        UserManager<ApplicationUser> userManager, EmailService emailService,
        IHubContext<OrderHub> hubContext, IConfiguration configuration)
    {
        _cart = cart;
        _db = db;
        _userManager = userManager;
        _emailService = emailService;
        _hubContext = hubContext;
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        ViewBag.CartCount = _cart.GetCartCount();
        return View(_cart.GetCart());
    }

    [HttpPost]
    public IActionResult UpdateQuantity(int productId, int? variantId, int quantity)
    {
        if (quantity <= 0) _cart.RemoveItem(productId, variantId);
        else _cart.UpdateQuantity(productId, variantId, quantity);
        return Json(new { success = true, cartCount = _cart.GetCartCount() });
    }

    [HttpPost]
    public IActionResult Remove(int productId, int? variantId)
    {
        _cart.RemoveItem(productId, variantId);
        return Json(new { success = true, cartCount = _cart.GetCartCount() });
    }

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
    public IActionResult PlaceOrder(CheckoutViewModel vm)
    {
        if (!ModelState.IsValid) return View("Checkout", vm);

        var cartItems = _cart.GetCart();
        if (!cartItems.Any()) return RedirectToAction("Index");

        // Lưu thông tin vào Session
        HttpContext.Session.SetString("CheckoutInfo", JsonConvert.SerializeObject(vm));

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

        ViewBag.TotalAmount = totalAmount;
        ViewBag.DiscountAmount = discountAmount;
        ViewBag.FinalAmount = totalAmount - discountAmount;
        ViewBag.CartItems = cartItems;
        ViewBag.CheckoutInfo = vm;

        return View("ConfirmPayment");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessPayment()
    {
        try
        {
            var cartItems = _cart.GetCart();
            if (!cartItems.Any()) return RedirectToAction("Index");

            var checkoutInfoJson = HttpContext.Session.GetString("CheckoutInfo");
            if (string.IsNullOrEmpty(checkoutInfoJson)) return RedirectToAction("Checkout");

            var vm = JsonConvert.DeserializeObject<CheckoutViewModel>(checkoutInfoJson);
            if (vm == null) return RedirectToAction("Checkout");

            // TẠO ĐƠN HÀNG
            var order = new Order
            {
                GuestFullName = vm.FullName,
                GuestEmail = vm.Email,
                GuestPhone = vm.Phone,
                ShippingAddress = vm.ShippingAddress,
                Note = vm.Note,
                CouponCode = vm.CouponCode,
                TotalAmount = cartItems.Sum(x => x.Subtotal),
                PaymentMethod = vm.PaymentMethod,
                IsPaid = false,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending
            };

            // Tính toán giảm giá
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

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                order.UserId = user?.Id;
            }

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

            // RẼ NHÁNH XỬ LÝ THANH TOÁN
            if (order.PaymentMethod == "VNPAY")
            {
                var vnpay = new VnPayLibrary();
                var vnp_TmnCode = _configuration["VnPay:TmnCode"];
                var vnp_HashSecret = _configuration["VnPay:HashSecret"];
                var vnp_Url = _configuration["VnPay:BaseUrl"];

                // FIX LỖI vnp_Amount: Nhân 100 và làm tròn về số nguyên long
                long finalAmount = (long)Math.Round((order.TotalAmount - order.DiscountAmount) * 100);

                vnpay.AddRequestData("vnp_Version", "2.1.0");
                vnpay.AddRequestData("vnp_Command", "pay");
                vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
                vnpay.AddRequestData("vnp_Amount", finalAmount.ToString());
                vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
                vnpay.AddRequestData("vnp_CurrCode", "VND");
                vnpay.AddRequestData("vnp_IpAddr", "127.0.0.1");
                vnpay.AddRequestData("vnp_Locale", "vn");
                vnpay.AddRequestData("vnp_OrderInfo", "Thanh-toan-don-hang-" + order.Id);
                vnpay.AddRequestData("vnp_OrderType", "other");
                vnpay.AddRequestData("vnp_ReturnUrl", Url.Action("PaymentCallback", "Cart", null, Request.Scheme));
                vnpay.AddRequestData("vnp_TxnRef", order.Id.ToString());

                string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
                return Redirect(paymentUrl);
            }

            await CompleteOrderLogic(order);
            return RedirectToAction("PaymentSuccess", new { id = order.Id });
        }
        catch (Exception ex)
        {
            return BadRequest($"Lỗi: {ex.Message}");
        }
    }

    [HttpGet]
    public async Task<IActionResult> PaymentCallback()
    {
        var vnpay = new VnPayLibrary();
        var vnp_HashSecret = _configuration["VnPay:HashSecret"];

        foreach (var (key, value) in Request.Query)
        {
            if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                vnpay.AddResponseData(key, value);
        }

        string orderIdStr = vnpay.GetResponseData("vnp_TxnRef");
        string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
        string vnp_SecureHash = Request.Query["vnp_SecureHash"];

        if (vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret) && vnp_ResponseCode == "00")
        {
            var order = await _db.Orders.FindAsync(int.Parse(orderIdStr));
            if (order != null)
            {
                order.IsPaid = true;
                order.Status = OrderStatus.Confirmed;
                await _db.SaveChangesAsync();
                await CompleteOrderLogic(order);
                return RedirectToAction("PaymentSuccess", new { id = order.Id });
            }
        }
        return RedirectToAction("PaymentFailed");
    }

    private async Task CompleteOrderLogic(Order order)
    {
        _cart.ClearCart();
        HttpContext.Session.Remove("CheckoutInfo");
        try
        {
            await _emailService.SendOrderConfirmationAsync(order.GuestEmail, order.GuestFullName, order.Id, order.TotalAmount - order.DiscountAmount);

            // Cập nhật cục dữ liệu gửi lên cái chuông (thêm isGuest)
            var orderNotification = new
            {
                orderId = order.Id,
                customerName = order.GuestFullName,
                amount = order.TotalAmount - order.DiscountAmount,
                isGuest = order.UserId == null // true nếu khách chưa đăng nhập
            };

            await _hubContext.Clients.Group("AdminGroup").SendAsync("NewOrder", orderNotification);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Lỗi SignalR: " + ex.Message);
        }
    }

    public IActionResult PaymentSuccess(int id)
    {
        var order = _db.Orders.Find(id);
        if (order == null) return BadRequest("Đơn hàng không tồn tại");
        ViewBag.OrderId = id;
        return View();
    }
}