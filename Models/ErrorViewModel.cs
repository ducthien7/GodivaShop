// Models/ErrorViewModel.cs
namespace GodivaShop.Web.Models;   // ← Phải đúng namespace này

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}