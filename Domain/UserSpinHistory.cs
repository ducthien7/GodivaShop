using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GodivaShop.Web.Models.Domain
{
    public class UserSpinHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        [Required]
        public int PrizeId { get; set; }

        [ForeignKey("PrizeId")]
        public LuckyPrize Prize { get; set; }

        public DateTime SpinDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string GeneratedCode { get; set; } // Mã Coupon được sinh ra (nếu trúng voucher)
    }
}