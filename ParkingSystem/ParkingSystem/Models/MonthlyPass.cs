using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkingSystem.Models
{
    public class MonthlyPass
    {
        [Key]
        public int PassId { get; set; }

        [Required]
        public int VehicleId { get; set; }

        [Required]
        [Display(Name = "Ngày bắt đầu")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "Ngày hết hạn")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,0)")]
        [Display(Name = "Giá vé")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá vé phải lớn hơn 0")]
        public decimal Price { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Active"; // Active | Expired | Cancelled

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("VehicleId")]
        public virtual Vehicle? Vehicle { get; set; }

        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
