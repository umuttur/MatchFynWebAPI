using MatchFynWebAPI.Models.Chat;

namespace MatchFynWebAPI.Services.Background
{
    /// <summary>
    /// Interface for matching algorithm service
    /// Handles user matching based on preferences and compatibility
    /// </summary>
    public interface IMatchingService
    {
        /// <summary>
        /// Find compatible users for matching rooms
        /// </summary>
        Task<List<string>> FindCompatibleUsersAsync(string userId, string genderFilter, int? minAge, int? maxAge);

        /// <summary>
        /// Calculate compatibility score between users
        /// </summary>
        Task<double> CalculateCompatibilityScoreAsync(string userId1, string userId2);

        /// <summary>
        /// Create optimized room groups based on compatibility
        /// </summary>
        Task<List<List<string>>> CreateOptimizedGroupsAsync(List<string> userIds, int groupSize);

        /// <summary>
        /// Handle user likes/dislikes in matching rooms
        /// </summary>
        Task ProcessUserReactionAsync(string userId, string targetUserId, bool isLike);

        /// <summary>
        /// Get user's match history and preferences
        /// </summary>
        Task<object> GetUserMatchingDataAsync(string userId);
    }
}
