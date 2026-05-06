using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Flyzone.Models
{
    public class TypingService 
    {
        public int Id { get; set; }
        
        [Required, MaxLength(100)]
        public string Category { get; set; } = null!;
        
        [Required, MaxLength(150)]
        public string ServiceName { get; set; } = null!;
        
        public string? Description { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseFee { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}