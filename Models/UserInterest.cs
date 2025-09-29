namespace MatchFynWebAPI.Models
{
    public class UserInterest
    {
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;
        
        public int InterestId { get; set; }
        public virtual Interest Interest { get; set; } = null!;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
