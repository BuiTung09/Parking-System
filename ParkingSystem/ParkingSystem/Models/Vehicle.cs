using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkingSystem.Models
{
    public class Vehicle
    {
        [Key]
        public int VehicleId { get; set; }

        [Required]
        public string OwnerId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Biển số xe không được để trống!")]
        [StringLength(15, ErrorMessage = "Biển số xe không được vượt quá 15 ký tự.")]
        [Display(Name = "Biển số xe")]
        public string PlateNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loại xe!")]
        [Display(Name = "Loại xe")]
        public string VehicleType { get; set; } = "Motorbike"; // Car | Motorbike

        [StringLength(50)]
        [Display(Name = "Hãng xe")]
        public string? Brand { get; set; }

        [StringLength(30)]
        [Display(Name = "Màu xe")]
        public string? Color { get; set; }

        [Display(Name = "Ngày đăng ký")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Đã xóa")]
        public bool IsDeleted { get; set; } = false;

        // Navigation Properties
        [ForeignKey("OwnerId")]
        public virtual ApplicationUser? Owner { get; set; }

        public virtual ICollection<MonthlyPass> MonthlyPasses { get; set; } = new List<MonthlyPass>();
        public virtual ICollection<ParkingLog> ParkingLogs { get; set; } = new List<ParkingLog>();
    }
}
