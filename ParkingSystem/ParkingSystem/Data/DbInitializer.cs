using Microsoft.AspNetCore.Identity;
using ParkingSystem.Models;

namespace ParkingSystem.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Tạo Roles
            string[] roles = { "Admin", "Staff", "Member" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Tạo tài khoản Admin mặc định
            var adminEmail = "admin@parking.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Quản Trị Viên",
                    PhoneNumber = "0901234567",
                    IsActive = true,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            // 3. Tạo tài khoản Staff mặc định
            var staffEmail = "staff1@parking.com";
            if (await userManager.FindByEmailAsync(staffEmail) == null)
            {
                var staff = new ApplicationUser
                {
                    UserName = staffEmail,
                    Email = staffEmail,
                    FullName = "Nguyễn Văn Bảo Vệ",
                    PhoneNumber = "0912345678",
                    IsActive = true,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(staff, "Staff@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(staff, "Staff");
                }
            }

            // 4. Tạo tài khoản Member mặc định
            var memberEmail = "member1@parking.com";
            if (await userManager.FindByEmailAsync(memberEmail) == null)
            {
                var member = new ApplicationUser
                {
                    UserName = memberEmail,
                    Email = memberEmail,
                    FullName = "Trần Văn Khách",
                    PhoneNumber = "0923456789",
                    IsActive = true,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(member, "Member@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(member, "Member");
                }
            }

            // 5. Tạo cấu hình bãi xe mặc định
            if (!context.ParkingConfigs.Any())
            {
                context.ParkingConfigs.Add(new ParkingConfig
                {
                    TotalCarSlots = 50,
                    TotalBikeSlots = 200,
                    CarHourlyRate = 30000,
                    BikeHourlyRate = 5000,
                    CarMonthlyRate = 1500000,
                    BikeMonthlyRate = 200000,
                    UpdatedAt = DateTime.Now
                });
                await context.SaveChangesAsync();
            }

            // 6. Tạo phân khu mặc định
            if (!context.ParkingZones.Any())
            {
                context.ParkingZones.AddRange(
                    new ParkingZone { ZoneName = "Khu A", TotalCarSlots = 30, TotalBikeSlots = 100, Description = "Khu vực ngoài trời phía trước" },
                    new ParkingZone { ZoneName = "Khu B", TotalCarSlots = 20, TotalBikeSlots = 100, Description = "Khu vực có mái che phía sau" }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}
