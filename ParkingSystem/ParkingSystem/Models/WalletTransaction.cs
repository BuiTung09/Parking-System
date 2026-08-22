using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkingSystem.Models
{
    public class WalletTransaction
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,0)")]
        [Display(Name = "Số tiền")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Loại giao dịch")]
        public string Type { get; set; } = "TopUp"; // TopUp | Spend

        [StringLength(200)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        public int? PaymentId { get; set; }

        [Column(TypeName = "decimal(18,0)")]
        [Display(Name = "Số dư sau giao dịch")]
        public decimal BalanceAfter { get; set; }

        [Display(Name = "Thời gian")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey("PaymentId")]
        public virtual Payment? Payment { get; set; }
    }
}
