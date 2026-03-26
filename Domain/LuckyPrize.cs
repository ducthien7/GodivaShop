using System.ComponentModel.DataAnnotations;

namespace GodivaShop.Web.Models.Domain
{
    public class LuckyPrize
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } // Tên hiển thị trên ô (VD: Voucher 10%, Freeship)

        public decimal? DiscountValue { get; set; } // Giá trị giảm (nếu là Voucher)
        public int? Points { get; set; } // Số điểm (nếu trúng điểm tích lũy)

        [Required]
        public double WinChance { get; set; } // Tỷ lệ trúng thưởng (%), ví dụ: 5.0 nghĩa là 5%

        public int Quantity { get; set; } // Số lượng quà còn lại (-1 là vô hạn)

        [StringLength(7)]
        public string FillColor { get; set; } // Màu nền của ô đó (VD: #C1A35E)

        public bool IsActive { get; set; } = true;
    }
}