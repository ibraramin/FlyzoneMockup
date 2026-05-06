using System.ComponentModel.DataAnnotations;

namespace Flyzone.Models.Forms
{
    public class DrivingLicenseRenewalForm 
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string ServiceApplicationId { get; set; } = null!;
        public ServiceApplication Application { get; set; } = null!;
        
        [Required, MaxLength(50)]
        public string TrafficFileNumber { get; set; } = null!;
        
        [Required]
        public DateTime CurrentLicenseExpiry { get; set; }
        
        [Required, MaxLength(100)]
        public string EyeTestCenterName { get; set; } = null!;
    }
}