using Flyzone.Models.Forms;
using System.ComponentModel.DataAnnotations;

namespace Flyzone.Models
{
    public class ServiceApplication 
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        
        public int ServiceId { get; set; }
        public TypingService Service { get; set; } = null!;
        
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;
        
        public string? InternalNotes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public ICollection<ApplicationHistory> StatusHistory { get; set; } = null!;
        public ICollection<UserDocument> AttachedDocuments { get; set; } = null!;

        public GoldenVisaForm? GoldenVisaDetails { get; set; }
        public DrivingLicenseRenewalForm? DrivingLicenseDetails { get; set; }
    }
}