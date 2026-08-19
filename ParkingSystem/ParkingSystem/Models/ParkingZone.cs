using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Models
{
    public class ParkingZone
    {
        [Key]
        public int ZoneId { get; set; }

        [Required(ErrorMessage = "Tên phân khu không được để trống!")]
        [StringLength(50, ErrorMessage = "Tên phân khu không được vượt quá 50 ký tự.")]
        [Display(Name = "Tên phân khu")]
        public string ZoneName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số slot ô tô không được để trống!")]
        [Range(0, 10000, ErrorMessage = "Số slot ô tô phải từ 0 đến 10000")]
        [Display(Name = "Tổng slot Ô tô")]
        public int TotalCarSlots { get; set; } = 50;

        [Required(ErrorMessage = "Số slot xe máy không được để trống!")]
        [Range(0, 10000, ErrorMessage = "Số slot xe máy phải từ 0 đến 10000")]
        [Display(Name = "Tổng slot Xe máy")]
        public int TotalBikeSlots { get; set; } = 200;

        [StringLength(200, ErrorMessage = "Mô tả không được vượt quá 200 ký tự.")]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        public virtual ICollection<ParkingLog> ParkingLogs { get; set; } = new List<ParkingLog>();
    }
}
