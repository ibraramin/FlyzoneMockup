using System.ComponentModel.DataAnnotations;

namespace Flyzone.Models.Forms
{
    public class GoldenVisaForm 
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string ServiceApplicationId { get; set; } = null!;
        public ServiceApplication Application { get; set; } = null!;
        
        [Required, MaxLength(50)]
        public string PassportNumber { get; set; } = null!;
        
        [MaxLength(20)]
        public string? CurrentEmiratesId { get; set; }
        
        [Required, MaxLength(100)]
        public string Profession { get; set; } = null!;
        
        public decimal MonthlySalary { get; set; }
        
        public bool IsInvestorCategory { get; set; }
    }
}