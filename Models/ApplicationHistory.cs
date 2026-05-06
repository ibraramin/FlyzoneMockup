using System.ComponentModel.DataAnnotations;

namespace Flyzone.Models
{
    public class ApplicationHistory 
    {
        public int Id { get; set; }
        
        [Required]
        public string ServiceApplicationId { get; set; } = null!;
        public ServiceApplication Application { get; set; } = null!;
        
        public ApplicationStatus StatusState { get; set; }
        public string? Comments { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}