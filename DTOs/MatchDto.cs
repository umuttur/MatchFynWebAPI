using System.ComponentModel.DataAnnotations;

namespace MatchFynWebAPI.DTOs
{
    public class MatchDto
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string? SenderProfileImage { get; set; }
        public int ReceiverId { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string? ReceiverProfileImage { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public string? Message { get; set; }
    }

    public class CreateMatchDto
    {
        [Required]
        public int ReceiverId { get; set; }
        
        [StringLength(500)]
        public string? Message { get; set; }
    }

    public class RespondToMatchDto
    {
        [Required]
        [RegularExpression("^(accepted|rejected)$", ErrorMessage = "Status must be 'accepted' or 'rejected'")]
        public string Status { get; set; } = string.Empty;
    }
}
