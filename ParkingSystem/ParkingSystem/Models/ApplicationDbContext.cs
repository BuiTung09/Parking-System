using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ParkingSystem.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<MonthlyPass> MonthlyPasses { get; set; }
        public DbSet<ParkingLog> ParkingLogs { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<ParkingConfig> ParkingConfigs { get; set; }
        public DbSet<ParkingZone> ParkingZones { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Vehicle>(entity =>
            {
                entity.HasIndex(v => v.PlateNumber).IsUnique();

                entity.HasOne(v => v.Owner)
                    .WithMany()
                    .HasForeignKey(v => v.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(v => v.VehicleType)
                    .HasMaxLength(20);

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_Vehicle_Type", "[VehicleType] IN ('Car', 'Motorbike')"));
            });

            builder.Entity<MonthlyPass>(entity =>
            {
                entity.HasOne(m => m.Vehicle)
                    .WithMany(v => v.MonthlyPasses)
                    .HasForeignKey(m => m.VehicleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_MonthlyPass_Status", "[Status] IN ('Active', 'Expired', 'Cancelled')"));

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_MonthlyPass_Price", "[Price] >= 0"));
            });

            builder.Entity<ParkingLog>(entity =>
            {
                entity.HasIndex(p => p.PlateNumber);
                entity.HasIndex(p => p.Status);
                entity.HasIndex(p => p.TimeIn);

                entity.HasOne(p => p.Vehicle)
                    .WithMany(v => v.ParkingLogs)
                    .HasForeignKey(p => p.VehicleId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(p => p.StaffIn)
                    .WithMany()
                    .HasForeignKey(p => p.StaffInId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.StaffOut)
                    .WithMany()
                    .HasForeignKey(p => p.StaffOutId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(p => p.VehicleType)
                    .HasMaxLength(20);

                entity.HasOne(p => p.Zone)
                    .WithMany(z => z.ParkingLogs)
                    .HasForeignKey(p => p.ZoneId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_ParkingLog_Status", "[Status] IN ('Inside', 'Outside', 'Waiting')"));

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_ParkingLog_VehicleType", "[VehicleType] IN ('Car', 'Motorbike')"));
            });

            builder.Entity<Payment>(entity =>
            {
                entity.HasOne(p => p.ParkingLog)
                    .WithOne(l => l.Payment)
                    .HasForeignKey<Payment>(p => p.ParkingLogId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.MonthlyPass)
                    .WithMany(m => m.Payments)
                    .HasForeignKey(p => p.MonthlyPassId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.User)
                    .WithMany()
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_Payment_Method", "[PaymentMethod] IN ('Cash', 'EWallet', 'BankTransfer', 'Wallet')"));

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_Payment_Status", "[Status] IN ('Pending', 'Completed', 'Failed')"));

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_Payment_Amount", "[Amount] >= 0"));
            });

            builder.Entity<ParkingConfig>(entity =>
            {
                entity.HasOne(c => c.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(c => c.UpdatedBy)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_Config_CarSlots", "[TotalCarSlots] > 0"));

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_Config_BikeSlots", "[TotalBikeSlots] > 0"));
            });

            builder.Entity<WalletTransaction>(entity =>
            {
                entity.HasOne(t => t.User)
                    .WithMany(u => u.WalletTransactions)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.Payment)
                    .WithMany()
                    .HasForeignKey(t => t.PaymentId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_WalletTx_Type", "[Type] IN ('TopUp', 'Spend')"));
            });
        }
    }
}
