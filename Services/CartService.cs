using GodivaShop.Web.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace GodivaShop.Web.Services;

public class CartService
{
    private const string CartKey = "GODIVA_CART";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CartService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ISession Session => _httpContextAccessor.HttpContext!.Session;

    public List<CartItem> GetCart()
    {
        var json = Session.GetString(CartKey);
        return json == null ? new List<CartItem>()
            : JsonConvert.DeserializeObject<List<CartItem>>(json)!;
    }

    public void SaveCart(List<CartItem> cart)
    {
        Session.SetString(CartKey, JsonConvert.SerializeObject(cart));
    }

    public void AddItem(CartItem newItem)
    {
        var cart = GetCart();
        var existing = cart.FirstOrDefault(x =>
            x.ProductId == newItem.ProductId && x.VariantId == newItem.VariantId);

        if (existing != null)
            existing.Quantity += newItem.Quantity;
        else
            cart.Add(newItem);

        SaveCart(cart);
    }

    public void UpdateQuantity(int productId, int? variantId, int quantity)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(x =>
            x.ProductId == productId && x.VariantId == variantId);

        if (item != null)
        {
            if (quantity <= 0) cart.Remove(item);
            else item.Quantity = quantity;
        }
        SaveCart(cart);
    }

    public void RemoveItem(int productId, int? variantId)
    {
        var cart = GetCart();
        cart.RemoveAll(x => x.ProductId == productId && x.VariantId == variantId);
        SaveCart(cart);
    }

    public void ClearCart() => Session.Remove(CartKey);

    public int GetCartCount() => GetCart().Sum(x => x.Quantity);
}