using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkingSystem.Models
{
    public class ParkingLog
    {
        [Key]
        public int LogId { get; set; }

        // Nullable — xe vãng lai không cần liên kết Vehicle
        public int? VehicleId { get; set; }

        // Staff thực hiện check-in
        public string? StaffInId { get; set; }

        // Staff thực hiện check-out
        public string? StaffOutId { get; set; }

        [Required(ErrorMessage = "Biển số xe không được để trống!")]
        [StringLength(15, ErrorMessage = "Biển số xe không được vượt quá 15 ký tự.")]
        [Display(Name = "Biển số xe")]
        public string PlateNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loại xe!")]
        [Display(Name = "Loại xe")]
        public string VehicleType { get; set; } = "Motorbike"; // Car | Motorbike

        [Required]
        [Display(Name = "Thời gian vào")]
        [DataType(DataType.DateTime)]
        public DateTime TimeIn { get; set; } = DateTime.Now;

        [Display(Name = "Thời gian ra")]
        [DataType(DataType.DateTime)]
        public DateTime? TimeOut { get; set; }

        [Column(TypeName = "decimal(18,0)")]
        [Display(Name = "Phí đỗ xe")]
        public decimal? Fee { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Inside"; // Inside | Outside | Waiting

        [Display(Name = "Vé tháng")]
        public bool IsMonthlyPass { get; set; } = false;

        [StringLength(200)]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        // Navigation Properties
        [ForeignKey("VehicleId")]
        public virtual Vehicle? Vehicle { get; set; }

        [ForeignKey("StaffInId")]
        public virtual ApplicationUser? StaffIn { get; set; }

        [ForeignKey("StaffOutId")]
        public virtual ApplicationUser? StaffOut { get; set; }

        public virtual Payment? Payment { get; set; }

        public int? ZoneId { get; set; }

        [ForeignKey("ZoneId")]
        public virtual ParkingZone? Zone { get; set; }
    }
}