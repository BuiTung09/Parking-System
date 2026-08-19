using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkingSystem.Models
{
    public class ParkingConfig
    {
        [Key]
        public int ConfigId { get; set; }

        [Required]
        [Range(1, 10000, ErrorMessage = "Số slot ô tô phải từ 1 đến 10000")]
        [Display(Name = "Tổng slot Ô tô")]
        public int TotalCarSlots { get; set; } = 50;

        [Required]
        [Range(1, 10000, ErrorMessage = "Số slot xe máy phải từ 1 đến 10000")]
        [Display(Name = "Tổng slot Xe máy")]
        public int TotalBikeSlots { get; set; } = 200;

        [Required]
        [Column(TypeName = "decimal(18,0)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        [Display(Name = "Giá Ô tô / giờ (VND)")]
        public decimal CarHourlyRate { get; set; } = 30000;

        [Required]
        [Column(TypeName = "decimal(18,0)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        [Display(Name = "Giá Xe máy / giờ (VND)")]
        public decimal BikeHourlyRate { get; set; } = 5000;

        [Required]
        [Column(TypeName = "decimal(18,0)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        [Display(Name = "Vé tháng Ô tô (VND)")]
        public decimal CarMonthlyRate { get; set; } = 1500000;

        [Required]
        [Column(TypeName = "decimal(18,0)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        [Display(Name = "Vé tháng Xe máy (VND)")]
        public decimal BikeMonthlyRate { get; set; } = 200000;

        [Display(Name = "Cập nhật lúc")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public string? UpdatedBy { get; set; }

        // Navigation
        [ForeignKey("UpdatedBy")]
        public virtual ApplicationUser? UpdatedByUser { get; set; }
    }
}
