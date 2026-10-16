using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Models;
using ParkingSystem.Models.ViewModels;
using ParkingSystem.Services;

namespace ParkingSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminController> _logger;
        private readonly ParkingSlotService _slotService;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<AdminController> logger,
            ParkingSlotService slotService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _slotService = slotService;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var allLogs = await _context.ParkingLogs.ToListAsync();
            var members = await _userManager.GetUsersInRoleAsync("Member");

            // === FIX: Tính doanh thu từ bảng Payments (bao gồm cả vé tháng + phí đỗ xe) ===
            var allPayments = await _context.Payments
                .Where(p => p.Status == "Completed" && (p.MonthlyPassId != null || p.ParkingLogId != null))
                .ToListAsync();
            var totalRevenue = allPayments.Sum(p => p.Amount);

            // Dữ liệu biểu đồ 7 ngày gần nhất - từ bảng Payments
            var last7Days = Enumerable.Range(0, 7).Select(i => DateTime.Today.AddDays(-6 + i)).ToList();
            var chartLabels = last7Days.Select(d => d.ToString("dd/MM")).ToList();
            var chartRevenue = last7Days.Select(d =>
                allPayments.Where(p => p.CreatedAt.Date == d)
                       .Sum(p => p.Amount)).ToList();
            var chartCheckIns = last7Days.Select(d =>
                allLogs.Count(l => l.TimeIn.Date == d)).ToList();
            var chartCheckOuts = last7Days.Select(d =>
                allLogs.Count(l => l.TimeOut.HasValue && l.TimeOut.Value.Date == d)).ToList();

            var model = new AdminDashboardViewModel
            {
                TotalRevenue = totalRevenue,
                TotalVehiclesServed = allLogs.Count(l => l.Status == "Outside"),
                CurrentInsideCount = allLogs.Count(l => l.Status == "Inside"),
                TotalMembers = members.Count,
                ActiveMonthlyPasses = await _context.MonthlyPasses.CountAsync(m => m.Status == "Active" && m.EndDate >= DateTime.Now),
                AdminName = currentUser?.FullName ?? "Admin",
                ChartLabels = chartLabels,
                ChartRevenue = chartRevenue,
                ChartCheckIns = chartCheckIns,
                ChartCheckOuts = chartCheckOuts
            };

            _logger.LogInformation("[ADMIN] {Admin} truy cập Dashboard.", currentUser?.FullName);
            return View(model);
        }

        public async Task<IActionResult> ManageStaff()
        {
            var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
            var model = staffUsers.Select(u => new StaffListViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email!,
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            }).ToList();

            ViewData["ActivePage"] = "ManageStaff";
            return View(model);
        }

        [HttpGet]
        public IActionResult CreateStaff()
        {
            ViewData["ActivePage"] = "ManageStaff";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStaff(CreateStaffViewModel model)
        {
            ViewData["ActivePage"] = "ManageStaff";
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    IsActive = true,
                    EmailConfirmed = true
                };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Staff");
                    _logger.LogInformation("[ADMIN] Tạo tài khoản Staff mới: {Email} bởi {Admin}.",
                        model.Email, User.Identity?.Name);
                    TempData["Success"] = $"Tạo tài khoản {model.FullName} thành công!";
                    return RedirectToAction(nameof(ManageStaff));
                }
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStaff(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            var status = user.IsActive ? "kích hoạt" : "khóa";
            _logger.LogInformation("[ADMIN] {Admin} đã {Status} tài khoản Staff {Email}.",
                User.Identity?.Name, status, user.Email);
            TempData["Success"] = $"Đã {status} tài khoản {user.FullName}!";
            return RedirectToAction(nameof(ManageStaff));
        }

        public async Task<IActionResult> ManageMembers()
        {
            var memberUsers = await _userManager.GetUsersInRoleAsync("Member");
            var memberIds = memberUsers.Select(u => u.Id).ToList();
            var vehicleCounts = await _context.Vehicles
                .Where(v => memberIds.Contains(v.OwnerId) && !v.IsDeleted)
                .GroupBy(v => v.OwnerId)
                .Select(g => new { OwnerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.OwnerId, x => x.Count);

            var model = memberUsers
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new MemberListViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email!,
                    PhoneNumber = u.PhoneNumber,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    VehicleCount = vehicleCounts.GetValueOrDefault(u.Id)
                }).ToList();

            ViewData["ActivePage"] = "ManageMembers";
            return View(model);
        }

        [HttpGet]
        public IActionResult CreateMember()
        {
            ViewData["ActivePage"] = "ManageMembers";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMember(RegisterViewModel model)
        {
            ViewData["ActivePage"] = "ManageMembers";
            if (ModelState.IsValid)
            {
                var existingPhone = await _userManager.Users
                    .AnyAsync(u => u.PhoneNumber == model.PhoneNumber);
                if (existingPhone)
                {
                    ModelState.AddModelError(nameof(model.PhoneNumber), "Số điện thoại đã được sử dụng.");
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    UserName = model.PhoneNumber,
                    Email = model.Email.Trim(),
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    IsActive = true,
                    EmailConfirmed = true
                };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Member");
                    _logger.LogInformation("[ADMIN] Tạo tài khoản Member mới: {Phone} bởi {Admin}.",
                        model.PhoneNumber, User.Identity?.Name);
                    TempData["Success"] = $"Tạo tài khoản thành viên {model.FullName} thành công!";
                    return RedirectToAction(nameof(ManageMembers));
                }
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleMember(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (!await _userManager.IsInRoleAsync(user, "Member"))
                return BadRequest();

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            var status = user.IsActive ? "kích hoạt" : "khóa";
            _logger.LogInformation("[ADMIN] {Admin} đã {Status} tài khoản Member {Phone}.",
                User.Identity?.Name, status, user.PhoneNumber);
            TempData["Success"] = $"Đã {status} tài khoản {user.FullName}!";
            return RedirectToAction(nameof(ManageMembers));
        }

        [HttpGet]
        public async Task<IActionResult> ParkingConfig()
        {
            ViewData["ActivePage"] = "ParkingConfig";
            var config = await _context.ParkingConfigs.FirstOrDefaultAsync() ?? new ParkingConfig();

            var zoneCarTotal = await _context.ParkingZones.SumAsync(z => (int?)z.TotalCarSlots) ?? 0;
            var zoneBikeTotal = await _context.ParkingZones.SumAsync(z => (int?)z.TotalBikeSlots) ?? 0;
            ViewData["ZoneCarTotal"] = zoneCarTotal;
            ViewData["ZoneBikeTotal"] = zoneBikeTotal;
            ViewData["ZoneCount"] = await _context.ParkingZones.CountAsync();

            return View(config);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ParkingConfig(ParkingConfig model)
        {
            ViewData["ActivePage"] = "ParkingConfig";

            // Validate: tổng slot cấu hình mới không được nhỏ hơn tổng slot đã phân bổ cho các khu
            var zoneCarTotal = await _context.ParkingZones.SumAsync(z => (int?)z.TotalCarSlots) ?? 0;
            var zoneBikeTotal = await _context.ParkingZones.SumAsync(z => (int?)z.TotalBikeSlots) ?? 0;
            if (model.TotalCarSlots < zoneCarTotal)
                ModelState.AddModelError("TotalCarSlots",
                    $"Tổng slot ô tô không được nhỏ hơn tổng slot đã phân bổ cho các khu ({zoneCarTotal} slot). Hãy giảm slot các khu xuống trước.");
            if (model.TotalBikeSlots < zoneBikeTotal)
                ModelState.AddModelError("TotalBikeSlots",
                    $"Tổng slot xe máy không được nhỏ hơn tổng slot đã phân bổ cho các khu ({zoneBikeTotal} slot). Hãy giảm slot các khu xuống trước.");

            if (ModelState.IsValid)
            {
                var existing = await _context.ParkingConfigs.FirstOrDefaultAsync();
                if (existing != null)
                {
                    existing.TotalCarSlots = model.TotalCarSlots;
                    existing.TotalBikeSlots = model.TotalBikeSlots;
                    existing.CarHourlyRate = model.CarHourlyRate;
                    existing.BikeHourlyRate = model.BikeHourlyRate;
                    existing.CarMonthlyRate = model.CarMonthlyRate;
                    existing.BikeMonthlyRate = model.BikeMonthlyRate;
                    existing.UpdatedAt = DateTime.Now;
                    var currentUser = await _userManager.GetUserAsync(User);
                    existing.UpdatedBy = currentUser?.Id;
                    _context.ParkingConfigs.Update(existing);
                }
                else
                {
                    model.UpdatedAt = DateTime.Now;
                    _context.ParkingConfigs.Add(model);
                }
                await _context.SaveChangesAsync();

                _logger.LogInformation("[ADMIN] {Admin} cập nhật cấu hình bãi xe: Car={CarSlots}, Bike={BikeSlots}.",
                    User.Identity?.Name, model.TotalCarSlots, model.TotalBikeSlots);
                TempData["Success"] = "Cập nhật cấu hình bãi đỗ thành công!";
                return RedirectToAction(nameof(ParkingConfig));
            }

            ViewData["ZoneCarTotal"] = zoneCarTotal;
            ViewData["ZoneBikeTotal"] = zoneBikeTotal;
            ViewData["ZoneCount"] = await _context.ParkingZones.CountAsync();
            return View(model);
        }

        // === FIX: Revenue Report tính từ bảng Payments (bao gồm vé tháng + phí đỗ) ===
        public async Task<IActionResult> RevenueReport(string period = "day", DateTime? from = null, DateTime? to = null)
        {
            ViewData["ActivePage"] = "RevenueReport";
            var fromDate = from ?? DateTime.Today.AddDays(-30);
            var toDate = to ?? DateTime.Today;

            // Query từ Payments thay vì ParkingLogs
            var payments = await _context.Payments
                .Where(p => p.Status == "Completed"
                    && (p.MonthlyPassId != null || p.ParkingLogId != null)
                    && p.CreatedAt.Date >= fromDate.Date && p.CreatedAt.Date <= toDate.Date)
                .Include(p => p.ParkingLog)
                .Include(p => p.MonthlyPass)
                    .ThenInclude(mp => mp!.Vehicle)
                .ToListAsync();

            var model = new RevenueReportViewModel
            {
                Period = period,
                FromDate = fromDate,
                ToDate = toDate,
                TotalRevenue = payments.Sum(p => p.Amount),
                TotalTransactions = payments.Count
            };

            if (period == "day")
            {
                var days = Enumerable.Range(0, (toDate - fromDate).Days + 1).Select(i => fromDate.AddDays(i)).ToList();
                model.Labels = days.Select(d => d.ToString("dd/MM")).ToList();
                model.RevenueData = days.Select(d => payments.Where(p => p.CreatedAt.Date == d).Sum(p => p.Amount)).ToList();
                model.CarData = days.Select(d => payments.Count(p => p.CreatedAt.Date == d && GetVehicleType(p) == "Car")).ToList();
                model.BikeData = days.Select(d => payments.Count(p => p.CreatedAt.Date == d && GetVehicleType(p) == "Motorbike")).ToList();
            }
            else if (period == "week")
            {
                var weeks = payments.GroupBy(p => System.Globalization.CultureInfo.CurrentCulture.Calendar
                    .GetWeekOfYear(p.CreatedAt, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                    .OrderBy(g => g.Key).ToList();
                model.Labels = weeks.Select(w => $"Tuần {w.Key}").ToList();
                model.RevenueData = weeks.Select(w => w.Sum(p => p.Amount)).ToList();
                model.CarData = weeks.Select(w => w.Count(p => GetVehicleType(p) == "Car")).ToList();
                model.BikeData = weeks.Select(w => w.Count(p => GetVehicleType(p) == "Motorbike")).ToList();
            }
            else
            {
                var months = payments.GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month).ToList();
                model.Labels = months.Select(m => $"{m.Key.Month}/{m.Key.Year}").ToList();
                model.RevenueData = months.Select(m => m.Sum(p => p.Amount)).ToList();
                model.CarData = months.Select(m => m.Count(p => GetVehicleType(p) == "Car")).ToList();
                model.BikeData = months.Select(m => m.Count(p => GetVehicleType(p) == "Motorbike")).ToList();
            }

            _logger.LogInformation("[ADMIN] Xem báo cáo doanh thu: {Period} từ {From} đến {To}.", period, fromDate, toDate);
            return View(model);
        }

        // Helper: lấy VehicleType từ Payment (qua ParkingLog hoặc MonthlyPass)
        private static string GetVehicleType(Payment p)
        {
            if (p.ParkingLog != null) return p.ParkingLog.VehicleType;
            if (p.MonthlyPass?.Vehicle != null) return p.MonthlyPass.Vehicle.VehicleType;
            return "Motorbike";
        }

        public async Task<IActionResult> TrafficReport(string period = "day", DateTime? from = null, DateTime? to = null)
        {
            ViewData["ActivePage"] = "TrafficReport";
            var fromDate = from ?? DateTime.Today.AddDays(-30);
            var toDate = to ?? DateTime.Today;

            var logs = await _context.ParkingLogs
                .Where(l => l.TimeIn.Date >= fromDate.Date && l.TimeIn.Date <= toDate.Date)
                .ToListAsync();

            var model = new RevenueReportViewModel
            {
                Period = period,
                FromDate = fromDate,
                ToDate = toDate,
                TotalTransactions = logs.Count
            };

            var days = Enumerable.Range(0, (toDate - fromDate).Days + 1).Select(i => fromDate.AddDays(i)).ToList();
            model.Labels = days.Select(d => d.ToString("dd/MM")).ToList();
            model.CarData = days.Select(d => logs.Count(l => l.TimeIn.Date == d && l.VehicleType == "Car")).ToList();
            model.BikeData = days.Select(d => logs.Count(l => l.TimeIn.Date == d && l.VehicleType == "Motorbike")).ToList();

            _logger.LogInformation("[ADMIN] Xem báo cáo lưu lượng: {Period} từ {From} đến {To}.", period, fromDate, toDate);
            return View(model);
        }

        public async Task<IActionResult> StaffActivity(DateTime? from, DateTime? to, string? staffId)
        {
            ViewData["ActivePage"] = "StaffActivity";
            var fromDate = (from ?? DateTime.Today).Date;
            var toDate = (to ?? fromDate).Date;
            if (toDate < fromDate)
                (fromDate, toDate) = (toDate, fromDate);

            var rangeStart = fromDate;
            var rangeEndExclusive = toDate.AddDays(1);

            var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
            var staffList = staffUsers.Select(u => new StaffListViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            }).ToList();

            var logs = await _context.ParkingLogs
                .Where(l =>
                    (l.TimeIn >= rangeStart && l.TimeIn < rangeEndExclusive)
                    || (l.TimeOut != null && l.TimeOut >= rangeStart && l.TimeOut < rangeEndExclusive))
                .Include(l => l.StaffIn)
                .Include(l => l.StaffOut)
                .Include(l => l.Vehicle)
                    .ThenInclude(v => v!.Owner)
                .ToListAsync();

            var activities = new List<StaffActivityItem>();

            foreach (var log in logs)
            {
                var memberName = log.Vehicle?.Owner?.FullName;

                if (log.TimeIn >= rangeStart && log.TimeIn < rangeEndExclusive)
                {
                    activities.Add(new StaffActivityItem
                    {
                        Time = log.TimeIn,
                        ActionType = "CheckIn",
                        PlateNumber = log.PlateNumber,
                        VehicleType = log.VehicleType,
                        IsMonthlyPass = log.IsMonthlyPass,
                        StaffName = log.StaffIn?.FullName ?? "Hệ thống",
                        StaffId = log.StaffInId ?? string.Empty,
                        Fee = null,
                        MemberName = memberName
                    });
                }

                if (log.TimeOut != null && log.TimeOut >= rangeStart && log.TimeOut < rangeEndExclusive)
                {
                    activities.Add(new StaffActivityItem
                    {
                        Time = log.TimeOut.Value,
                        ActionType = "CheckOut",
                        PlateNumber = log.PlateNumber,
                        VehicleType = log.VehicleType,
                        IsMonthlyPass = log.IsMonthlyPass,
                        StaffName = log.StaffOut?.FullName ?? "Hệ thống",
                        StaffId = log.StaffOutId ?? string.Empty,
                        Fee = log.Fee,
                        MemberName = memberName
                    });
                }
            }

            if (!string.IsNullOrEmpty(staffId))
            {
                activities = activities.Where(a => a.StaffId == staffId).ToList();
            }

            activities = activities.OrderByDescending(a => a.Time).ToList();

            var model = new StaffActivityViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                SelectedStaffId = staffId,
                StaffList = staffList,
                Activities = activities,
                TotalCheckIns = activities.Count(a => a.ActionType == "CheckIn"),
                TotalCheckOuts = activities.Count(a => a.ActionType == "CheckOut")
            };

            _logger.LogInformation("[ADMIN] Xem nhật ký hoạt động nhân viên từ {From} đến {To}.",
                fromDate.ToString("yyyy-MM-dd"), toDate.ToString("yyyy-MM-dd"));
            return View(model);
        }

        public async Task<IActionResult> ManageZones()
        {
            ViewData["ActivePage"] = "ManageZones";

            var config = await _context.ParkingConfigs.FirstOrDefaultAsync() ?? new ParkingConfig();
            var zones = await _context.ParkingZones.ToListAsync();
            var insideLogs = await _context.ParkingLogs.Where(l => l.Status == "Inside").ToListAsync();

            var zoneCarTotal = zones.Sum(z => z.TotalCarSlots);
            var zoneBikeTotal = zones.Sum(z => z.TotalBikeSlots);
            ViewData["ConfigCarTotal"] = config.TotalCarSlots;
            ViewData["ConfigBikeTotal"] = config.TotalBikeSlots;
            ViewData["ZoneCarTotal"] = zoneCarTotal;
            ViewData["ZoneBikeTotal"] = zoneBikeTotal;
            ViewData["RemainingCarSlots"] = config.TotalCarSlots - zoneCarTotal;
            ViewData["RemainingBikeSlots"] = config.TotalBikeSlots - zoneBikeTotal;

            var model = zones.Select(z => new ZoneStatusViewModel
            {
                ZoneId = z.ZoneId,
                ZoneName = z.ZoneName,
                TotalCarSlots = z.TotalCarSlots,
                TotalBikeSlots = z.TotalBikeSlots,
                CurrentCarCount = insideLogs.Count(l => l.ZoneId == z.ZoneId && l.VehicleType == "Car"),
                CurrentBikeCount = insideLogs.Count(l => l.ZoneId == z.ZoneId && l.VehicleType == "Motorbike"),
                Description = z.Description
            }).ToList();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CreateZone()
        {
            ViewData["ActivePage"] = "ManageZones";
            var config = await _context.ParkingConfigs.FirstOrDefaultAsync() ?? new ParkingConfig();
            var zoneCarTotal = await _context.ParkingZones.SumAsync(z => (int?)z.TotalCarSlots) ?? 0;
            var zoneBikeTotal = await _context.ParkingZones.SumAsync(z => (int?)z.TotalBikeSlots) ?? 0;
            ViewData["RemainingCarSlots"] = config.TotalCarSlots - zoneCarTotal;
            ViewData["RemainingBikeSlots"] = config.TotalBikeSlots - zoneBikeTotal;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateZone(ParkingZone zone)
        {
            ViewData["ActivePage"] = "ManageZones";
            var config = await _context.ParkingConfigs.FirstOrDefaultAsync() ?? new ParkingConfig();
            var zoneCarTotal = await _context.ParkingZones.SumAsync(z => (int?)z.TotalCarSlots) ?? 0;
            var zoneBikeTotal = await _context.ParkingZones.SumAsync(z => (int?)z.TotalBikeSlots) ?? 0;

            if (zoneCarTotal + zone.TotalCarSlots > config.TotalCarSlots)
                ModelState.AddModelError("TotalCarSlots",
                    $"Vượt quá giới hạn! Chỉ còn {config.TotalCarSlots - zoneCarTotal} slot ô tô chưa phân bổ (cấu hình tổng: {config.TotalCarSlots} slot).");
            if (zoneBikeTotal + zone.TotalBikeSlots > config.TotalBikeSlots)
                ModelState.AddModelError("TotalBikeSlots",
                    $"Vượt quá giới hạn! Chỉ còn {config.TotalBikeSlots - zoneBikeTotal} slot xe máy chưa phân bổ (cấu hình tổng: {config.TotalBikeSlots} slot).");

            if (ModelState.IsValid)
            {
                _context.ParkingZones.Add(zone);
                await _context.SaveChangesAsync();
                _logger.LogInformation("[ADMIN] Tạo phân khu mới: {ZoneName}.", zone.ZoneName);
                TempData["Success"] = $"Tạo phân khu {zone.ZoneName} thành công!";
                return RedirectToAction(nameof(ManageZones));
            }
            ViewData["RemainingCarSlots"] = config.TotalCarSlots - zoneCarTotal;
            ViewData["RemainingBikeSlots"] = config.TotalBikeSlots - zoneBikeTotal;
            return View(zone);
        }

        [HttpGet]
        public async Task<IActionResult> EditZone(int id)
        {
            ViewData["ActivePage"] = "ManageZones";
            var zone = await _context.ParkingZones.FindAsync(id);
            if (zone == null) return NotFound();

            var config = await _context.ParkingConfigs.FirstOrDefaultAsync() ?? new ParkingConfig();
            // remaining = config total - (sum of other zones, excluding current)
            var otherCarTotal = await _context.ParkingZones.Where(z => z.ZoneId != id).SumAsync(z => (int?)z.TotalCarSlots) ?? 0;
            var otherBikeTotal = await _context.ParkingZones.Where(z => z.ZoneId != id).SumAsync(z => (int?)z.TotalBikeSlots) ?? 0;
            ViewData["RemainingCarSlots"] = config.TotalCarSlots - otherCarTotal;
            ViewData["RemainingBikeSlots"] = config.TotalBikeSlots - otherBikeTotal;
            return View(zone);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditZone(ParkingZone zone)
        {
            ViewData["ActivePage"] = "ManageZones";
            var config = await _context.ParkingConfigs.FirstOrDefaultAsync() ?? new ParkingConfig();
            var otherCarTotal = await _context.ParkingZones.Where(z => z.ZoneId != zone.ZoneId).SumAsync(z => (int?)z.TotalCarSlots) ?? 0;
            var otherBikeTotal = await _context.ParkingZones.Where(z => z.ZoneId != zone.ZoneId).SumAsync(z => (int?)z.TotalBikeSlots) ?? 0;

            if (otherCarTotal + zone.TotalCarSlots > config.TotalCarSlots)
                ModelState.AddModelError("TotalCarSlots",
                    $"Vượt quá giới hạn! Các khu khác đã dùng {otherCarTotal} slot, cấu hình tổng là {config.TotalCarSlots} slot. Khu này chỉ có thể đặt tối đa {config.TotalCarSlots - otherCarTotal} slot ô tô.");
            if (otherBikeTotal + zone.TotalBikeSlots > config.TotalBikeSlots)
                ModelState.AddModelError("TotalBikeSlots",
                    $"Vượt quá giới hạn! Các khu khác đã dùng {otherBikeTotal} slot, cấu hình tổng là {config.TotalBikeSlots} slot. Khu này chỉ có thể đặt tối đa {config.TotalBikeSlots - otherBikeTotal} slot xe máy.");

            if (ModelState.IsValid)
            {
                var existing = await _context.ParkingZones.FindAsync(zone.ZoneId);
                if (existing == null) return NotFound();

                existing.ZoneName = zone.ZoneName;
                existing.TotalCarSlots = zone.TotalCarSlots;
                existing.TotalBikeSlots = zone.TotalBikeSlots;
                existing.Description = zone.Description;

                _context.ParkingZones.Update(existing);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[ADMIN] Cập nhật phân khu {ZoneId}: {ZoneName}.", zone.ZoneId, zone.ZoneName);
                TempData["Success"] = $"Cập nhật phân khu {zone.ZoneName} thành công!";
                return RedirectToAction(nameof(ManageZones));
            }
            ViewData["RemainingCarSlots"] = config.TotalCarSlots - otherCarTotal;
            ViewData["RemainingBikeSlots"] = config.TotalBikeSlots - otherBikeTotal;
            return View(zone);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteZone(int id)
        {
            var zone = await _context.ParkingZones.FindAsync(id);
            if (zone == null) return NotFound();

            var hasVehicles = await _context.ParkingLogs.AnyAsync(l => l.ZoneId == id && l.Status == "Inside");
            if (hasVehicles)
            {
                TempData["Error"] = $"Không thể xóa phân khu {zone.ZoneName} khi còn xe đang đỗ bên trong!";
                return RedirectToAction(nameof(ManageZones));
            }

            _context.ParkingZones.Remove(zone);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[ADMIN] Xóa phân khu {ZoneId}: {ZoneName}.", id, zone.ZoneName);
            TempData["Success"] = $"Đã xóa phân khu {zone.ZoneName} thành công!";
            return RedirectToAction(nameof(ManageZones));
        }
    }
}