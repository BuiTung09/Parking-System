using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Models.ViewModels
{
    // ===== Account ViewModels =====
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email hoặc Số điện thoại không được để trống")]
        [Display(Name = "Email hoặc Số điện thoại")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100)]
        [Display(Name = "Họ và tên đăng ký")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;


        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // ===== Staff ViewModels =====
    public class CheckInViewModel
    {
        [Required(ErrorMessage = "Biển số xe không được để trống!")]
        [StringLength(15)]
        [Display(Name = "Biển số xe")]
        public string PlateNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loại xe!")]
        [Display(Name = "Loại xe")]
        public string VehicleType { get; set; } = "Motorbike";

        [Required(ErrorMessage = "Vui lòng chọn phân khu đỗ!")]
        [Display(Name = "Phân khu đỗ")]
        public int ZoneId { get; set; }
    }

    public class StaffDashboardViewModel
    {
        public List<ParkingLog> InsideVehicles { get; set; } = new();
        public List<ParkingLog> WaitingLogs { get; set; } = new();
        public ParkingStatusViewModel ParkingStatus { get; set; } = new();
        public CheckInViewModel CheckInForm { get; set; } = new();
        public List<ParkingZone> Zones { get; set; } = new();
    }

    public class ParkingStatusViewModel
    {
        public int TotalCarSlots { get; set; } = 50;
        public int TotalBikeSlots { get; set; } = 200;
        public int CurrentCarCount { get; set; }
        public int CurrentBikeCount { get; set; }
        public int AvailableCarSlots => TotalCarSlots - CurrentCarCount;
        public int AvailableBikeSlots => TotalBikeSlots - CurrentBikeCount;
        public bool IsCarFull => AvailableCarSlots <= 0;
        public bool IsBikeFull => AvailableBikeSlots <= 0;
        public bool IsFull => IsCarFull && IsBikeFull;
        public List<ZoneStatusViewModel> ZoneStatuses { get; set; } = new();
    }

    public class ZoneStatusViewModel
    {
        public int ZoneId { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public int TotalCarSlots { get; set; }
        public int TotalBikeSlots { get; set; }
        public int CurrentCarCount { get; set; }
        public int CurrentBikeCount { get; set; }
        public string? Description { get; set; }
        public int AvailableCarSlots => TotalCarSlots - CurrentCarCount;
        public int AvailableBikeSlots => TotalBikeSlots - CurrentBikeCount;
        public bool IsCarFull => AvailableCarSlots <= 0;
        public bool IsBikeFull => AvailableBikeSlots <= 0;
    }

    // ===== Admin ViewModels =====
    public class AdminDashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalVehiclesServed { get; set; }
        public int CurrentInsideCount { get; set; }
        public int TotalMembers { get; set; }
        public int ActiveMonthlyPasses { get; set; }
        public string AdminName { get; set; } = string.Empty;

        // Dữ liệu cho biểu đồ
        public List<string> ChartLabels { get; set; } = new();
        public List<decimal> ChartRevenue { get; set; } = new();
        public List<int> ChartCheckIns { get; set; } = new();
        public List<int> ChartCheckOuts { get; set; } = new();
    }

    public class CreateStaffViewModel
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;
    }

    public class StaffListViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MemberListViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int VehicleCount { get; set; }
    }

    public class RevenueReportViewModel
    {
        public string Period { get; set; } = "day"; // day | week | month
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public List<string> Labels { get; set; } = new();
        public List<decimal> RevenueData { get; set; } = new();
        public List<int> CarData { get; set; } = new();
        public List<int> BikeData { get; set; } = new();
    }

    // ===== Member ViewModels =====
    public class MemberDashboardViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<Vehicle> Vehicles { get; set; } = new();
        public List<MonthlyPass> ActivePasses { get; set; } = new();
        public List<ParkingLog> WaitingLogs { get; set; } = new();
        public int TotalParkingCount { get; set; }
        public decimal WalletBalance { get; set; }
    }

    public class RequestParkingViewModel
    {
        [Required(ErrorMessage = "Biển số xe không được để trống!")]
        [StringLength(15)]
        [Display(Name = "Biển số xe")]
        public string PlateNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loại xe!")]
        [Display(Name = "Loại xe")]
        public string VehicleType { get; set; } = "Motorbike";
    }

    public class AddVehicleViewModel
    {
        [Required(ErrorMessage = "Biển số xe không được để trống!")]
        [StringLength(15)]
        [Display(Name = "Biển số xe")]
        public string PlateNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loại xe!")]
        [Display(Name = "Loại xe")]
        public string VehicleType { get; set; } = "Motorbike";

        [StringLength(50)]
        [Display(Name = "Hãng xe")]
        public string? Brand { get; set; }

        [StringLength(30)]
        [Display(Name = "Màu xe")]
        public string? Color { get; set; }
    }

    public class RenewPassViewModel
    {
        public int VehicleId { get; set; }
        public string PlateNumber { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public decimal Price { get; set; }

        public decimal WalletBalance { get; set; }

        // Thông tin vé hiện tại (nếu có)
        public MonthlyPass? CurrentPass { get; set; }
    }

    public class TopUpWalletViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập số tiền nạp")]
        [Range(10000, 10000000, ErrorMessage = "Số tiền nạp từ 10.000 đến 10.000.000 VND")]
        [Display(Name = "Số tiền nạp")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        [Display(Name = "Phương thức thanh toán")]
        public string PaymentMethod { get; set; } = "EWallet";

        public decimal CurrentBalance { get; set; }
    }

    public class TopUpQrViewModel
    {
        public int PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "EWallet";
        public string TransferContent { get; set; } = string.Empty;
        public string QrImageUrl { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
    }

    public class WalletViewModel
    {
        public decimal Balance { get; set; }
        public List<WalletTransaction> Transactions { get; set; } = new();
    }

    public class StaffActivityViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? SelectedStaffId { get; set; }
        public List<StaffListViewModel> StaffList { get; set; } = new();
        public List<StaffActivityItem> Activities { get; set; } = new();
        
        public int TotalCheckIns { get; set; }
        public int TotalCheckOuts { get; set; }
    }

    public class StaffActivityItem
    {
        public DateTime Time { get; set; }
        public string ActionType { get; set; } = string.Empty; // "CheckIn" hoặc "CheckOut"
        public string PlateNumber { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public bool IsMonthlyPass { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string StaffId { get; set; } = string.Empty;
        public decimal? Fee { get; set; }
        public string? MemberName { get; set; }
    }
}
