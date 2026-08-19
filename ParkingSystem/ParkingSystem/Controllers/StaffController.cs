using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Models;
using ParkingSystem.Models.ViewModels;
using ParkingSystem.Services;

namespace ParkingSystem.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StaffController> _logger;
        private readonly ParkingSlotService _slotService;
        private readonly ParkingFeeCalculator _feeCalculator;
        private readonly UserManager<ApplicationUser> _userManager;

        public StaffController(
            ApplicationDbContext context,
            ILogger<StaffController> logger,
            ParkingSlotService slotService,
            ParkingFeeCalculator feeCalculator,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _slotService = slotService;
            _feeCalculator = feeCalculator;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var insideVehicles = await _context.ParkingLogs
                .Include(p => p.Zone)
                .Where(p => p.Status == "Inside")
                .OrderByDescending(p => p.TimeIn)
                .ToListAsync();

            var waitingLogs = await _context.ParkingLogs
                .Include(p => p.Zone)
                .Where(p => p.Status == "Waiting")
                .OrderBy(p => p.TimeIn)
                .ToListAsync();

            var model = new StaffDashboardViewModel
            {
                InsideVehicles = insideVehicles,
                WaitingLogs = waitingLogs,
                ParkingStatus = _slotService.GetCurrentStatus(),
                CheckInForm = new CheckInViewModel(),
                Zones = await _context.ParkingZones.ToListAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveCheckIn(int id, int zoneId)
        {
            var log = await _context.ParkingLogs.FindAsync(id);
            if (log == null || log.Status != "Waiting")
            {
                TempData["Error"] = "Không tìm thấy xe đang chờ!";
                return RedirectToAction(nameof(Index));
            }

            if (!_slotService.HasAvailableSlotInZone(zoneId, log.VehicleType))
            {
                var zoneName = _context.ParkingZones.Find(zoneId)?.ZoneName ?? "Phân khu";
                TempData["Error"] = $"{zoneName} đã đầy chỗ cho {(log.VehicleType == "Car" ? "Ô tô" : "Xe máy")}!";
                return RedirectToAction(nameof(Index));
            }

            var currentUser = await _userManager.GetUserAsync(User);
            
            // Check monthly pass status for this vehicle
            bool isMonthly = await _context.Vehicles
                .AnyAsync(v => v.PlateNumber == log.PlateNumber
                    && v.MonthlyPasses.Any(m => m.Status == "Active" && m.EndDate >= DateTime.Now));

            log.Status = "Inside";
            log.TimeIn = DateTime.Now;
            log.StaffInId = currentUser?.Id;
            log.IsMonthlyPass = isMonthly;
            log.ZoneId = zoneId;

            _context.ParkingLogs.Update(log);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã duyệt xe {log.PlateNumber} vào bãi thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn([Bind(Prefix = "CheckInForm")] CheckInViewModel form)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra phân khu còn slot không
                if (!_slotService.HasAvailableSlotInZone(form.ZoneId, form.VehicleType))
                {
                    var zoneName = _context.ParkingZones.Find(form.ZoneId)?.ZoneName ?? "Phân khu";
                    _logger.LogWarning("[CHECK-IN FAILED] Phân khu {ZoneName} đầy cho loại {VehicleType}.", zoneName, form.VehicleType);
                    TempData["Error"] = $"{zoneName} đã đầy chỗ cho {(form.VehicleType == "Car" ? "Ô tô" : "Xe máy")}!";
                    return RedirectToAction(nameof(Index));
                }

                // Kiểm tra xe đã ở trong bãi chưa
                var duplicate = await _context.ParkingLogs
                    .AnyAsync(p => p.PlateNumber == form.PlateNumber && p.Status == "Inside");
                if (duplicate)
                {
                    _logger.LogWarning("[CHECK-IN FAILED] Xe {PlateNumber} đã có trong bãi.", form.PlateNumber);
                    TempData["Error"] = $"Xe {form.PlateNumber} đã có trong bãi!";
                    return RedirectToAction(nameof(Index));
                }

                var currentUser = await _userManager.GetUserAsync(User);

                // Kiểm tra vé tháng
                bool isMonthly = await _context.Vehicles
                    .AnyAsync(v => v.PlateNumber == form.PlateNumber
                        && v.MonthlyPasses.Any(m => m.Status == "Active" && m.EndDate >= DateTime.Now));

                var log = new ParkingLog
                {
                    PlateNumber = form.PlateNumber,
                    VehicleType = form.VehicleType,
                    TimeIn = DateTime.Now,
                    Status = "Inside",
                    StaffInId = currentUser?.Id,
                    IsMonthlyPass = isMonthly,
                    Fee = null,
                    TimeOut = null,
                    ZoneId = form.ZoneId
                };

                // Liên kết Vehicle nếu tồn tại
                var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.PlateNumber == form.PlateNumber);
                if (vehicle != null) log.VehicleId = vehicle.VehicleId;

                _context.ParkingLogs.Add(log);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[CHECK-IN SUCCESS] Xe {PlateNumber} ({VehicleType}) vào bãi phân khu {ZoneId}. Vé tháng: {IsMonthly}. Staff: {Staff}.",
                    form.PlateNumber, form.VehicleType, form.ZoneId, isMonthly, currentUser?.FullName);

                TempData["Success"] = $"Xe {form.PlateNumber} đã vào bãi phân khu {_context.ParkingZones.Find(form.ZoneId)?.ZoneName} thành công!" + (isMonthly ? " (Vé tháng)" : "");
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(int id)
        {
            var log = await _context.ParkingLogs.FindAsync(id);
            if (log == null || log.Status == "Outside")
            {
                _logger.LogWarning("[CHECK-OUT FAILED] Không tìm thấy xe với LogId={LogId}.", id);
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var config = _slotService.GetConfig();

            log.TimeOut = DateTime.Now;
            log.Status = "Outside";
            log.StaffOutId = currentUser?.Id;
            log.Fee = _feeCalculator.Calculate(log, config);

            // Tạo Payment record
            var payment = new Payment
            {
                ParkingLogId = log.LogId,
                UserId = currentUser?.Id ?? "",
                Amount = log.Fee ?? 0,
                PaymentMethod = log.IsMonthlyPass ? "EWallet" : "Cash",
                Status = "Completed",
                CreatedAt = DateTime.Now
            };
            _context.Payments.Add(payment);

            _context.ParkingLogs.Update(log);
            await _context.SaveChangesAsync();



            _logger.LogInformation("[CHECK-OUT SUCCESS] Xe {PlateNumber} xuất bãi. Phí: {Fee} VND. Vé tháng: {IsMonthly}. Staff: {Staff}.",
                log.PlateNumber, log.Fee, log.IsMonthlyPass, currentUser?.FullName);

            TempData["Success"] = log.IsMonthlyPass
                ? $"Xe {log.PlateNumber} xuất bãi (Vé tháng - Miễn phí)"
                : $"Xe {log.PlateNumber} xuất bãi. Phí: {log.Fee:N0} VND";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> SearchVehicle(string? query)
        {
            var logs = new List<ParkingLog>();
            if (!string.IsNullOrWhiteSpace(query))
            {
                logs = await _context.ParkingLogs
                    .Where(p => p.PlateNumber.Contains(query))
                    .OrderByDescending(p => p.TimeIn)
                    .Take(50)
                    .ToListAsync();
                _logger.LogInformation("[SEARCH] Staff tìm kiếm xe: {Query}. Kết quả: {Count}.", query, logs.Count);
            }
            ViewData["Query"] = query;
            return View(logs);
        }

        public IActionResult GetParkingStatus()
        {
            var status = _slotService.GetCurrentStatus();
            return PartialView("_ParkingStatus", status);
        }
    }
}
