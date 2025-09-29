using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MatchFynWebAPI.Services.Background;

namespace MatchFynWebAPI.Controllers
{
    /// <summary>
    /// Matching controller for MatchFyn compatibility system
    /// Implements intelligent user matching and compatibility scoring
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MatchingController : ControllerBase
    {
        private readonly IMatchingService _matchingService;
        private readonly ILogger<MatchingController> _logger;

        public MatchingController(
            IMatchingService matchingService,
            ILogger<MatchingController> logger)
        {
            _matchingService = matchingService ?? throw new ArgumentNullException(nameof(matchingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Find compatible users for the current user
        /// </summary>
        [HttpGet("compatible-users")]
        public async Task<ActionResult<object>> GetCompatibleUsers(
            [FromQuery] string genderFilter = "Mixed",
            [FromQuery] int? minAge = null,
            [FromQuery] int? maxAge = null,
            [FromQuery] int limit = 20)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid user" });
                }

                var compatibleUsers = await _matchingService.FindCompatibleUsersAsync(
                    userId, genderFilter, minAge, maxAge);

                var limitedUsers = compatibleUsers.Take(limit).ToList();

                return Ok(new
                {
                    CompatibleUsers = limitedUsers,
                    TotalFound = compatibleUsers.Count,
                    Filters = new
                    {
                        GenderFilter = genderFilter,
                        MinAge = minAge,
                        MaxAge = maxAge,
                        Limit = limit
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting compatible users");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Calculate compatibility score between current user and target user
        /// </summary>
        [HttpGet("compatibility/{targetUserId}")]
        public async Task<ActionResult<object>> GetCompatibilityScore(string targetUserId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid user" });
                }

                if (userId == targetUserId)
                {
                    return BadRequest(new { message = "Cannot calculate compatibility with yourself" });
                }

                var compatibilityScore = await _matchingService.CalculateCompatibilityScoreAsync(
                    userId, targetUserId);

                return Ok(new
                {
                    UserId = userId,
                    TargetUserId = targetUserId,
                    CompatibilityScore = compatibilityScore,
                    CompatibilityLevel = GetCompatibilityLevel(compatibilityScore),
                    CalculatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating compatibility score");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Process user reaction (like/dislike) in matching room
        /// </summary>
        [HttpPost("react")]
        public async Task<ActionResult<object>> ProcessReaction([FromBody] UserReactionDto reactionDto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid user" });
                }

                if (userId == reactionDto.TargetUserId)
                {
                    return BadRequest(new { message = "Cannot react to yourself" });
                }

                await _matchingService.ProcessUserReactionAsync(
                    userId, reactionDto.TargetUserId, reactionDto.IsLike);

                return Ok(new
                {
                    Message = reactionDto.IsLike ? "User liked successfully" : "User disliked successfully",
                    UserId = userId,
                    TargetUserId = reactionDto.TargetUserId,
                    IsLike = reactionDto.IsLike,
                    ProcessedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing user reaction");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get current user's matching profile and statistics
        /// </summary>
        [HttpGet("profile")]
        public async Task<ActionResult<object>> GetMatchingProfile()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid user" });
                }

                var matchingData = await _matchingService.GetUserMatchingDataAsync(userId);

                return Ok(new
                {
                    MatchingProfile = matchingData,
                    RetrievedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user matching profile");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Create optimized groups for matching rooms
        /// </summary>
        [HttpPost("create-groups")]
        public async Task<ActionResult<object>> CreateOptimizedGroups([FromBody] CreateGroupsDto groupsDto)
        {
            try
            {
                if (groupsDto.UserIds == null || !groupsDto.UserIds.Any())
                {
                    return BadRequest(new { message = "User IDs are required" });
                }

                if (groupsDto.GroupSize < 2 || groupsDto.GroupSize > 20)
                {
                    return BadRequest(new { message = "Group size must be between 2 and 20" });
                }

                var optimizedGroups = await _matchingService.CreateOptimizedGroupsAsync(
                    groupsDto.UserIds, groupsDto.GroupSize);

                return Ok(new
                {
                    Groups = optimizedGroups,
                    TotalGroups = optimizedGroups.Count,
                    TotalUsers = groupsDto.UserIds.Count,
                    GroupSize = groupsDto.GroupSize,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating optimized groups");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get matching statistics and insights
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetMatchingStatistics()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid user" });
                }

                // Get user's matching data for statistics
                var matchingData = await _matchingService.GetUserMatchingDataAsync(userId);

                return Ok(new
                {
                    Statistics = new
                    {
                        UserProfile = matchingData,
                        MatchingTips = GetMatchingTips(),
                        PopularInterests = GetPopularInterests(),
                        OptimalAgeRange = GetOptimalAgeRange()
                    },
                    GeneratedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting matching statistics");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        #region Helper Methods

        private static string GetCompatibilityLevel(double score)
        {
            return score switch
            {
                >= 0.8 => "Excellent",
                >= 0.6 => "Very Good",
                >= 0.4 => "Good",
                >= 0.2 => "Fair",
                _ => "Low"
            };
        }

        private static List<string> GetMatchingTips()
        {
            return new List<string>
            {
                "Complete your profile with interests for better matches",
                "Add a profile photo to increase compatibility",
                "Be active in chat rooms to meet more people",
                "Use voice chat to make stronger connections",
                "Be genuine and authentic in your interactions"
            };
        }

        private static List<string> GetPopularInterests()
        {
            return new List<string>
            {
                "Müzik", "Spor", "Seyahat", "Yemek", "Sinema", 
                "Kitap", "Sanat", "Teknoloji", "Doğa", "Dans"
            };
        }

        private static object GetOptimalAgeRange()
        {
            return new
            {
                Recommendation = "±5 years from your age typically yields best compatibility",
                MinRecommended = -5,
                MaxRecommended = 5,
                Note = "Age preferences can be adjusted based on personal preference"
            };
        }

        #endregion
    }

    /// <summary>
    /// DTO for user reaction in matching rooms
    /// </summary>
    public class UserReactionDto
    {
        public string TargetUserId { get; set; } = string.Empty;
        public bool IsLike { get; set; }
    }

    /// <summary>
    /// DTO for creating optimized groups
    /// </summary>
    public class CreateGroupsDto
    {
        public List<string> UserIds { get; set; } = new List<string>();
        public int GroupSize { get; set; } = 10;
    }
}
