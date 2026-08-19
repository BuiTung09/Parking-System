using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Models;
using ParkingSystem.Models.ViewModels;
using ParkingSystem.Services;

namespace ParkingSystem.Controllers
{
    [Authorize(Roles = "Member")]
    public class MemberController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<MemberController> _logger;
        private readonly ParkingSlotService _slotService;

        public MemberController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<MemberController> logger,
            ParkingSlotService slotService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _slotService = slotService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var vehicles = await _context.Vehicles
                .Where(v => v.OwnerId == user.Id && !v.IsDeleted)
                .Include(v => v.MonthlyPasses)
                .ToListAsync();

            var vehicleIds = vehicles.Select(v => v.VehicleId).ToList();

            var model = new MemberDashboardViewModel
            {
                FullName = user.FullName,
                Email = user.Email!,
                Vehicles = vehicles,
                ActivePasses = await _context.MonthlyPasses
                    .Include(m => m.Vehicle)
                    .Where(m => vehicleIds.Contains(m.VehicleId) && m.Status == "Active" && m.EndDate >= DateTime.Now)
                    .ToListAsync(),
                WaitingLogs = await _context.ParkingLogs
                    .Include(l => l.Vehicle)
                    .Where(l => vehicleIds.Contains(l.VehicleId ?? 0) && l.Status == "Waiting")
                    .ToListAsync(),
                TotalParkingCount = await _context.ParkingLogs
                    .CountAsync(l => vehicleIds.Contains(l.VehicleId ?? 0))
            };

            return View(model);
        }



        public async Task<IActionResult> MyVehicles()
        {
            var user = await _userManager.GetUserAsync(User);
            var vehicles = await _context.Vehicles
                .Where(v => v.OwnerId == user!.Id && !v.IsDeleted)
                .ToListAsync();
            return View(vehicles);
        }

        [HttpGet]
        public IActionResult AddVehicle()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVehicle(AddVehicleViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);

                // Kiểm tra biển số trùng
                var exists = await _context.Vehicles.AnyAsync(v => v.PlateNumber == model.PlateNumber);
                if (exists)
                {
                    ModelState.AddModelError("PlateNumber", "Biển số xe đã tồn tại trong hệ thống!");
                    return View(model);
                }

                var vehicle = new Vehicle
                {
                    OwnerId = user!.Id,
                    PlateNumber = model.PlateNumber,
                    VehicleType = model.VehicleType,
                    Brand = model.Brand,
                    Color = model.Color,
                    CreatedAt = DateTime.Now
                };
                _context.Vehicles.Add(vehicle);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[MEMBER] {User} đăng ký xe mới: {Plate} ({Type}).",
                    user.FullName, model.PlateNumber, model.VehicleType);
                TempData["Success"] = $"Đăng ký xe {model.PlateNumber} thành công!";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var vehicle = await _context.Vehicles
                .Include(v => v.MonthlyPasses)
                .FirstOrDefaultAsync(v => v.VehicleId == id && v.OwnerId == user!.Id);

            if (vehicle == null || vehicle.IsDeleted) return NotFound();

            bool isInside = await _context.ParkingLogs.AnyAsync(p => p.VehicleId == vehicle.VehicleId && p.Status == "Inside");
            if (isInside)
            {
                TempData["Error"] = "Không thể xóa phương tiện khi xe đang đỗ trong bãi!";
                return RedirectToAction(nameof(MyVehicles));
            }

            vehicle.IsDeleted = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("[MEMBER] {User} xóa xe: {Plate}.", user!.FullName, vehicle.PlateNumber);
            TempData["Success"] = $"Đã xóa xe {vehicle.PlateNumber}!";
            return RedirectToAction(nameof(MyVehicles));
        }

        public async Task<IActionResult> MyPasses()
        {
            var user = await _userManager.GetUserAsync(User);
            var vehicleIds = await _context.Vehicles
                .Where(v => v.OwnerId == user!.Id && !v.IsDeleted)
                .Select(v => v.VehicleId)
                .ToListAsync();

            var passes = await _context.MonthlyPasses
                .Include(m => m.Vehicle)
                .Where(m => vehicleIds.Contains(m.VehicleId))
                .OrderByDescending(m => m.EndDate)
                .ToListAsync();

            // Auto-expire
            foreach (var pass in passes.Where(p => p.Status == "Active" && p.EndDate < DateTime.Now))
            {
                pass.Status = "Expired";
            }
            await _context.SaveChangesAsync();

            return View(passes);
        }

        [HttpGet]
        public async Task<IActionResult> RenewPass(int vehicleId)
        {
            var user = await _userManager.GetUserAsync(User);
            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.VehicleId == vehicleId && v.OwnerId == user!.Id && !v.IsDeleted);
            if (vehicle == null) return NotFound();

            var config = _slotService.GetConfig();
            var currentPass = await _context.MonthlyPasses
                .Where(m => m.VehicleId == vehicleId && m.Status == "Active" && m.EndDate >= DateTime.Now)
                .FirstOrDefaultAsync();

            var model = new RenewPassViewModel
            {
                VehicleId = vehicle.VehicleId,
                PlateNumber = vehicle.PlateNumber,
                VehicleType = vehicle.VehicleType,
                Price = vehicle.VehicleType == "Car" ? config.CarMonthlyRate : config.BikeMonthlyRate,
                CurrentPass = currentPass
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RenewPass(RenewPassViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.VehicleId == model.VehicleId && v.OwnerId == user!.Id && !v.IsDeleted);
            if (vehicle == null) return NotFound();

            var config = _slotService.GetConfig();
            var price = vehicle.VehicleType == "Car" ? config.CarMonthlyRate : config.BikeMonthlyRate;

            // Tính ngày bắt đầu: từ ngày hiện tại hoặc từ ngày hết hạn vé cũ
            var currentPass = await _context.MonthlyPasses
                .Where(m => m.VehicleId == model.VehicleId && m.Status == "Active" && m.EndDate >= DateTime.Now)
                .FirstOrDefaultAsync();

            var startDate = currentPass != null ? currentPass.EndDate.AddDays(1) : DateTime.Today;
            var endDate = startDate.AddMonths(1);

            var pass = new MonthlyPass
            {
                VehicleId = model.VehicleId,
                StartDate = startDate,
                EndDate = endDate,
                Price = price,
                Status = "Active",
                CreatedAt = DateTime.Now
            };
            _context.MonthlyPasses.Add(pass);
            await _context.SaveChangesAsync();

            // Tạo thanh toán mock
            var payment = new Payment
            {
                MonthlyPassId = pass.PassId,
                UserId = user!.Id,
                Amount = price,
                PaymentMethod = model.PaymentMethod,
                Status = "Completed",
                CreatedAt = DateTime.Now
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[MEMBER] {User} gia hạn vé tháng xe {Plate}. Từ {Start} đến {End}. Phí: {Price}. PT: {Method}.",
                user.FullName, vehicle.PlateNumber, startDate.ToString("dd/MM/yyyy"), endDate.ToString("dd/MM/yyyy"), price, model.PaymentMethod);

            TempData["Success"] = $"Gia hạn vé tháng thành công! Hiệu lực: {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}";
            return RedirectToAction(nameof(MyPasses));
        }

        public async Task<IActionResult> ParkingHistory(int? vehicleId)
        {
            var user = await _userManager.GetUserAsync(User);
            var vehicleIds = await _context.Vehicles
                .Where(v => v.OwnerId == user!.Id && !v.IsDeleted)
                .Select(v => v.VehicleId)
                .ToListAsync();

            var query = _context.ParkingLogs
                .Where(l => vehicleIds.Contains(l.VehicleId ?? 0));

            if (vehicleId.HasValue)
                query = query.Where(l => l.VehicleId == vehicleId.Value);

            var logs = await query.OrderByDescending(l => l.TimeIn).Take(100).ToListAsync();

            ViewData["Vehicles"] = await _context.Vehicles
                .Where(v => v.OwnerId == user!.Id && !v.IsDeleted).ToListAsync();
            ViewData["SelectedVehicleId"] = vehicleId;
            return View(logs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestParking(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.VehicleId == id && v.OwnerId == user!.Id && !v.IsDeleted);

            if (vehicle == null) return NotFound();

            // Kiểm tra xem xe có đang đợi hoặc đang trong bãi không
            var isAlreadyParking = await _context.ParkingLogs
                .AnyAsync(p => p.VehicleId == vehicle.VehicleId && (p.Status == "Inside" || p.Status == "Waiting"));

            if (isAlreadyParking)
            {
                TempData["Error"] = $"Xe {vehicle.PlateNumber} đã gửi yêu cầu hoặc đang đỗ trong bãi!";
                return RedirectToAction(nameof(Index));
            }

            var log = new ParkingLog
            {
                PlateNumber = vehicle.PlateNumber,
                VehicleType = vehicle.VehicleType,
                TimeIn = DateTime.Now,
                Status = "Waiting",
                VehicleId = vehicle.VehicleId
            };
            _context.ParkingLogs.Add(log);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[MEMBER] {User} yêu cầu vào bãi cho xe: {Plate}.", user!.FullName, vehicle.PlateNumber);
            TempData["Success"] = $"Đã gửi yêu cầu vào bãi cho xe {vehicle.PlateNumber}. Vui lòng chạy xe đến cổng!";
            return RedirectToAction(nameof(Index));
        }
    }
}
