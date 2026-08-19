using ParkingSystem.Models;
using ParkingSystem.Models.ViewModels;

namespace ParkingSystem.Services
{
    public class ParkingFeeCalculator
    {
        /// <summary>
        /// Tính phí đỗ xe theo block thời gian thực
        /// </summary>
        public decimal Calculate(ParkingLog log, ParkingConfig config)
        {
            // Vé tháng không tính phí
            if (log.IsMonthlyPass) return 0;

            if (log.TimeOut == null) return 0;

            double totalHours = (log.TimeOut.Value - log.TimeIn).TotalHours;

            // Tối thiểu 1 giờ
            if (totalHours < 1) totalHours = 1;

            // Làm tròn lên theo giờ
            double roundedHours = Math.Ceiling(totalHours);

            if (log.VehicleType == "Car")
            {
                return (decimal)roundedHours * config.CarHourlyRate;
            }
            else
            {
                return (decimal)roundedHours * config.BikeHourlyRate;
            }
        }
    }

    public class ParkingSlotService
    {
        private readonly ApplicationDbContext _context;

        public ParkingSlotService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy trạng thái slot hiện tại (tính realtime từ DB)
        /// </summary>
        public ParkingStatusViewModel GetCurrentStatus()
        {
            var zones = _context.ParkingZones.ToList();
            var insideLogs = _context.ParkingLogs.Where(p => p.Status == "Inside").ToList();

            var totalCarSlots = zones.Any() ? zones.Sum(z => z.TotalCarSlots) : 50;
            var totalBikeSlots = zones.Any() ? zones.Sum(z => z.TotalBikeSlots) : 200;

            var zoneStatuses = zones.Select(z => new ZoneStatusViewModel
            {
                ZoneId = z.ZoneId,
                ZoneName = z.ZoneName,
                TotalCarSlots = z.TotalCarSlots,
                TotalBikeSlots = z.TotalBikeSlots,
                CurrentCarCount = insideLogs.Count(l => l.ZoneId == z.ZoneId && l.VehicleType == "Car"),
                CurrentBikeCount = insideLogs.Count(l => l.ZoneId == z.ZoneId && l.VehicleType == "Motorbike"),
                Description = z.Description
            }).ToList();

            return new ParkingStatusViewModel
            {
                TotalCarSlots = totalCarSlots,
                TotalBikeSlots = totalBikeSlots,
                CurrentCarCount = insideLogs.Count(v => v.VehicleType == "Car"),
                CurrentBikeCount = insideLogs.Count(v => v.VehicleType == "Motorbike"),
                ZoneStatuses = zoneStatuses
            };
        }

        /// <summary>
        /// Kiểm tra còn slot trống cho loại xe (toàn bãi)
        /// </summary>
        public bool HasAvailableSlot(string vehicleType)
        {
            var status = GetCurrentStatus();
            return vehicleType == "Car" ? !status.IsCarFull : !status.IsBikeFull;
        }

        /// <summary>
        /// Kiểm tra còn slot trống cho loại xe tại phân khu cụ thể
        /// </summary>
        public bool HasAvailableSlotInZone(int zoneId, string vehicleType)
        {
            var zone = _context.ParkingZones.Find(zoneId);
            if (zone == null) return false;

            var insideCount = _context.ParkingLogs
                .Count(p => p.Status == "Inside" && p.ZoneId == zoneId && p.VehicleType == vehicleType);

            return vehicleType == "Car" ? insideCount < zone.TotalCarSlots : insideCount < zone.TotalBikeSlots;
        }

        /// <summary>
        /// Lấy cấu hình bãi xe hiện tại
        /// </summary>
        public ParkingConfig GetConfig()
        {
            return _context.ParkingConfigs.FirstOrDefault() ?? new ParkingConfig();
        }
    }
}
