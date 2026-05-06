using System.ComponentModel.DataAnnotations;

namespace Flyzone.ViewModels
{
    public class DrivingLicenseViewModel
    {
        [Required(ErrorMessage = "Traffic file number is required")]
        [MaxLength(50, ErrorMessage = "Traffic file number cannot exceed 50 characters")]
        [Display(Name = "Traffic File Number")]
        public string TrafficFileNumber { get; set; } = null!;

        [Required(ErrorMessage = "Current license expiry date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Current License Expiry Date")]
        public DateTime CurrentLicenseExpiry { get; set; }

        [Required(ErrorMessage = "Eye test center name is required")]
        [MaxLength(100, ErrorMessage = "Eye test center name cannot exceed 100 characters")]
        [Display(Name = "Eye Test Center Name")]
        public string EyeTestCenterName { get; set; } = null!;
    }
}