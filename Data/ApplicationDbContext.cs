using Flyzone.Models;
using Flyzone.Models.Forms;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Flyzone.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TypingService> TypingServices { get; set; }
        public DbSet<ServiceApplication> ServiceApplications { get; set; }
        public DbSet<UserDocument> UserDocuments { get; set; }
        public DbSet<ApplicationHistory> ApplicationHistories { get; set; }
        
        // Form Tables
        public DbSet<GoldenVisaForm> GoldenVisaForms { get; set; }
        public DbSet<DrivingLicenseRenewalForm> DrivingLicenseRenewalForms { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1-to-1 Relationship: ServiceApplication <-> GoldenVisaForm
            builder.Entity<ServiceApplication>()
                .HasOne(a => a.GoldenVisaDetails)
                .WithOne(f => f.Application)
                .HasForeignKey<GoldenVisaForm>(f => f.ServiceApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            // 1-to-1 Relationship: ServiceApplication <-> DrivingLicenseRenewalForm
            builder.Entity<ServiceApplication>()
                .HasOne(a => a.DrivingLicenseDetails)
                .WithOne(f => f.Application)
                .HasForeignKey<DrivingLicenseRenewalForm>(f => f.ServiceApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed Data: Populate the catalog so we have something to test with
            builder.Entity<TypingService>().HasData(
                new TypingService { Id = 1, Category = "Visa & Immigration", ServiceName = "Golden Visa Application", BaseFee = 2800.00m, Description = "10-year residency visa processing." },
                new TypingService { Id = 2, Category = "RTA & Traffic", ServiceName = "Driving License Renewal", BaseFee = 320.00m, Description = "Renew your UAE driving license." }
            );
        }
    }
}