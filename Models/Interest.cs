using System.ComponentModel.DataAnnotations;

namespace MatchFynWebAPI.Models
{
    public class Interest
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [StringLength(50)]
        public string? Category { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual ICollection<UserInterest> UserInterests { get; set; } = new List<UserInterest>();
    }
}
