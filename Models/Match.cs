using System.ComponentModel.DataAnnotations;

namespace MatchFynWebAPI.Models
{
    public class Match
    {
        public int Id { get; set; }
        
        public int SenderId { get; set; }
        public virtual User Sender { get; set; } = null!;
        
        public int ReceiverId { get; set; }
        public virtual User Receiver { get; set; } = null!;
        
        [StringLength(20)]
        public string Status { get; set; } = "pending"; // pending, accepted, rejected
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? RespondedAt { get; set; }
        
        [StringLength(500)]
        public string? Message { get; set; }
    }
}
