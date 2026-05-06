using System.ComponentModel.DataAnnotations;

namespace Flyzone.ViewModels
{
    public class GoldenVisaViewModel
    {
        [Required(ErrorMessage = "Passport number is required")]
        [MaxLength(50, ErrorMessage = "Passport number cannot exceed 50 characters")]
        [Display(Name = "Passport Number")]
        public string PassportNumber { get; set; } = null!;

        [MaxLength(20, ErrorMessage = "Emirates ID cannot exceed 20 characters")]
        [Display(Name = "Current Emirates ID (Optional)")]
        public string? CurrentEmiratesId { get; set; }

        [Required(ErrorMessage = "Profession is required")]
        [MaxLength(100, ErrorMessage = "Profession cannot exceed 100 characters")]
        [Display(Name = "Profession")]
        public string Profession { get; set; } = null!;

        [Required(ErrorMessage = "Monthly salary is required")]
        [DataType(DataType.Currency)]
        [Display(Name = "Monthly Salary (AED)")]
        public decimal MonthlySalary { get; set; }

        [Display(Name = "Investor Category")]
        public bool IsInvestorCategory { get; set; }
    }
}