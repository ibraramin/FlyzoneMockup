using System.ComponentModel.DataAnnotations;

namespace Flyzone.Models
{
    public class UserDocument 
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        
        public string? ServiceApplicationId { get; set; }
        public ServiceApplication? Application { get; set; }
        
        [Required, MaxLength(50)]
        public string DocumentType { get; set; } = null!;
        
        [Required]
        public string FilePath { get; set; } = null!;
        
        public bool IsVerified { get; set; } = false;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}