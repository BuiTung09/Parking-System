using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkingSystem.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        // Thanh toán cho lần gửi xe (nullable)
        public int? ParkingLogId { get; set; }

        // Thanh toán cho vé tháng (nullable)
        public int? MonthlyPassId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,0)")]
        [Display(Name = "Số tiền")]
        [Range(0, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "Phương thức")]
        public string PaymentMethod { get; set; } = "Cash"; // Cash | EWallet | BankTransfer

        [Required]
        [StringLength(20)]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Completed"; // Pending | Completed | Failed

        [Display(Name = "Ngày thanh toán")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("ParkingLogId")]
        public virtual ParkingLog? ParkingLog { get; set; }

        [ForeignKey("MonthlyPassId")]
        public virtual MonthlyPass? MonthlyPass { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
    }
}
