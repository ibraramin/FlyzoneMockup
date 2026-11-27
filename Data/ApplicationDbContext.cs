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

            // Seed Data: the public catalog is read from these rows, not hard-coded cards.
            // Ids 1 and 2 are kept stable because the two application forms reference them.
            builder.Entity<TypingService>().HasData(
                new TypingService { Id = 1, Category = "Visa & Immigration", ServiceName = "Golden Visa Application", BaseFee = 2800.00m, Description = "10-year residency visa processing for investors and specialists." },
                new TypingService { Id = 2, Category = "RTA & Traffic", ServiceName = "Driving License Renewal", BaseFee = 320.00m, Description = "Renew your UAE driving license, including eye-test coordination." },
                new TypingService { Id = 3, Category = "Identity Documents", ServiceName = "Emirates ID Renewal", BaseFee = 180.00m, Description = "Renew or replace your Emirates ID with biometric capture." },
                new TypingService { Id = 4, Category = "Visa & Immigration", ServiceName = "Work Permit & Visa", BaseFee = 650.00m, Description = "New work permits, visa transfers and cancellations." },
                new TypingService { Id = 5, Category = "Business Services", ServiceName = "Business License Setup", BaseFee = 1450.00m, Description = "Company formation, trade-name reservation and license issuance." },
                new TypingService { Id = 6, Category = "Document Services", ServiceName = "Document Attestation", BaseFee = 120.00m, Description = "Certificate attestation, certified translation and courier handling." }
            );
        }
    }
}