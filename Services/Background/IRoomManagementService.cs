namespace MatchFynWebAPI.Services.Background
{
    /// <summary>
    /// Interface for room management background service
    /// Handles automatic room lifecycle management 7/24
    /// </summary>
    public interface IRoomManagementService
    {
        /// <summary>
        /// Clean up expired rooms
        /// </summary>
        Task CleanupExpiredRoomsAsync();

        /// <summary>
        /// Create automatic waiting rooms based on demand
        /// </summary>
        Task CreateWaitingRoomsAsync();

        /// <summary>
        /// Promote waiting rooms to matching rooms when full
        /// </summary>
        Task PromoteWaitingRoomsAsync();

        /// <summary>
        /// Handle inactive participants
        /// </summary>
        Task HandleInactiveParticipantsAsync();

        /// <summary>
        /// Create gender-based matching rooms
        /// </summary>
        Task CreateMatchingRoomsAsync();

        /// <summary>
        /// Monitor room health and performance
        /// </summary>
        Task MonitorRoomHealthAsync();
    }
}
