using Microsoft.EntityFrameworkCore;
using MatchFynWebAPI.Data;
using MatchFynWebAPI.Models.Chat;

namespace MatchFynWebAPI.Services.Background
{
    /// <summary>
    /// Matching algorithm service for MatchFyn
    /// Implements intelligent user matching based on preferences and compatibility
    /// </summary>
    public class MatchingService : IMatchingService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MatchingService> _logger;

        public MatchingService(
            IServiceScopeFactory scopeFactory,
            ILogger<MatchingService> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Find compatible users for matching rooms based on age, gender, and interests
        /// </summary>
        public async Task<List<string>> FindCompatibleUsersAsync(string userId, string genderFilter, int? minAge, int? maxAge)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var matchFynContext = scope.ServiceProvider.GetRequiredService<MatchFynDbContext>();
                var identityContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

                // Get user's profile and interests
                var currentUser = await identityContext.Users.FindAsync(userId);
                if (currentUser == null) return new List<string>();

                var currentUserAge = DateTime.UtcNow.Year - currentUser.DateOfBirth.Year;
                if (currentUser.DateOfBirth > DateTime.UtcNow.AddYears(-currentUserAge)) currentUserAge--;

                // Get user's interests
                var userInterests = await matchFynContext.UserInterests
                    .Where(ui => ui.User.Email == currentUser.Email)
                    .Select(ui => ui.InterestId)
                    .ToListAsync();

                // Find compatible users
                var compatibleUserIds = await identityContext.Users
                    .Where(u => u.Id != userId && 
                               u.IsActive && 
                               u.IsEmailVerified)
                    .Where(u => genderFilter == "Mixed" || 
                               (genderFilter == "Male" && u.Gender == "Male") ||
                               (genderFilter == "Female" && u.Gender == "Female"))
                    .Where(u => !minAge.HasValue || 
                               (DateTime.UtcNow.Year - u.DateOfBirth.Year) >= minAge)
                    .Where(u => !maxAge.HasValue || 
                               (DateTime.UtcNow.Year - u.DateOfBirth.Year) <= maxAge)
                    .Select(u => u.Id)
                    .ToListAsync();

                // Calculate compatibility scores and sort
                var compatibilityScores = new List<(string UserId, double Score)>();

                foreach (var compatibleUserId in compatibleUserIds)
                {
                    var score = await CalculateCompatibilityScoreAsync(userId, compatibleUserId);
                    compatibilityScores.Add((compatibleUserId, score));
                }

                var sortedUsers = compatibilityScores
                    .OrderByDescending(x => x.Score)
                    .Select(x => x.UserId)
                    .ToList();

                _logger.LogDebug("Found {Count} compatible users for user {UserId}", sortedUsers.Count, userId);
                return sortedUsers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding compatible users for {UserId}", userId);
                return new List<string>();
            }
        }

        /// <summary>
        /// Calculate compatibility score based on interests, age, and previous interactions
        /// </summary>
        public async Task<double> CalculateCompatibilityScoreAsync(string userId1, string userId2)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var matchFynContext = scope.ServiceProvider.GetRequiredService<MatchFynDbContext>();
                var identityContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

                var user1 = await identityContext.Users.FindAsync(userId1);
                var user2 = await identityContext.Users.FindAsync(userId2);

                if (user1 == null || user2 == null) return 0.0;

                double totalScore = 0.0;

                // Age compatibility (30% weight)
                var ageScore = CalculateAgeCompatibility(user1.DateOfBirth, user2.DateOfBirth);
                totalScore += ageScore * 0.3;

                // Interest compatibility (50% weight)
                var interestScore = await CalculateInterestCompatibilityAsync(user1.Email, user2.Email, matchFynContext);
                totalScore += interestScore * 0.5;

                // Location compatibility (10% weight)
                var locationScore = CalculateLocationCompatibility(user1.City, user2.City);
                totalScore += locationScore * 0.1;

                // Activity compatibility (10% weight)
                var activityScore = CalculateActivityCompatibility(user1.LastLoginAt, user2.LastLoginAt);
                totalScore += activityScore * 0.1;

                return Math.Round(totalScore, 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating compatibility score between {UserId1} and {UserId2}", userId1, userId2);
                return 0.0;
            }
        }

        /// <summary>
        /// Create optimized groups for matching rooms
        /// </summary>
        public async Task<List<List<string>>> CreateOptimizedGroupsAsync(List<string> userIds, int groupSize)
        {
            try
            {
                var groups = new List<List<string>>();
                var remainingUsers = new List<string>(userIds);

                while (remainingUsers.Count >= groupSize)
                {
                    var group = new List<string>();
                    
                    // Start with a random user
                    var firstUser = remainingUsers[Random.Shared.Next(remainingUsers.Count)];
                    group.Add(firstUser);
                    remainingUsers.Remove(firstUser);

                    // Add most compatible users to the group
                    while (group.Count < groupSize && remainingUsers.Any())
                    {
                        string? bestMatch = null;
                        double bestScore = 0.0;

                        foreach (var candidateUser in remainingUsers)
                        {
                            double totalCompatibility = 0.0;
                            
                            // Calculate average compatibility with all group members
                            foreach (var groupMember in group)
                            {
                                totalCompatibility += await CalculateCompatibilityScoreAsync(groupMember, candidateUser);
                            }

                            var averageCompatibility = totalCompatibility / group.Count;
                            
                            if (averageCompatibility > bestScore)
                            {
                                bestScore = averageCompatibility;
                                bestMatch = candidateUser;
                            }
                        }

                        if (bestMatch != null)
                        {
                            group.Add(bestMatch);
                            remainingUsers.Remove(bestMatch);
                        }
                        else
                        {
                            break; // No more compatible users
                        }
                    }

                    groups.Add(group);
                }

                // Handle remaining users (add to existing groups or create smaller group)
                if (remainingUsers.Any())
                {
                    if (groups.Any())
                    {
                        // Distribute remaining users to existing groups
                        for (int i = 0; i < remainingUsers.Count; i++)
                        {
                            var groupIndex = i % groups.Count;
                            groups[groupIndex].Add(remainingUsers[i]);
                        }
                    }
                    else
                    {
                        // Create a smaller group with remaining users
                        groups.Add(remainingUsers);
                    }
                }

                _logger.LogDebug("Created {GroupCount} optimized groups from {UserCount} users", groups.Count, userIds.Count);
                return groups;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating optimized groups");
                return new List<List<string>>();
            }
        }

        /// <summary>
        /// Process user reactions (likes/dislikes) for future matching improvements
        /// </summary>
        public async Task ProcessUserReactionAsync(string userId, string targetUserId, bool isLike)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var matchFynContext = scope.ServiceProvider.GetRequiredService<MatchFynDbContext>();
                var identityContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

                // Check if match already exists
                var existingMatch = await matchFynContext.Matches
                    .FirstOrDefaultAsync(m => 
                        (m.SenderId.ToString() == userId && m.ReceiverId.ToString() == targetUserId) ||
                        (m.SenderId.ToString() == targetUserId && m.ReceiverId.ToString() == userId));

                if (existingMatch == null)
                {
                    // Create new match record
                    var identityUser1 = await identityContext.Users.FindAsync(userId);
                    var identityUser2 = await identityContext.Users.FindAsync(targetUserId);
                    
                    var user1 = await matchFynContext.Users.FirstOrDefaultAsync(u => u.Email == identityUser1!.Email);
                    var user2 = await matchFynContext.Users.FirstOrDefaultAsync(u => u.Email == identityUser2!.Email);

                    if (user1 != null && user2 != null)
                    {
                        var match = new Models.Match
                        {
                            SenderId = user1.Id,
                            ReceiverId = user2.Id,
                            Status = isLike ? "accepted" : "rejected",
                            CreatedAt = DateTime.UtcNow,
                            RespondedAt = DateTime.UtcNow
                        };

                        matchFynContext.Matches.Add(match);
                        await matchFynContext.SaveChangesAsync();
                    }
                }

                _logger.LogInformation("Processed user reaction: {UserId} {Reaction} {TargetUserId}", 
                    userId, isLike ? "liked" : "disliked", targetUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing user reaction from {UserId} to {TargetUserId}", userId, targetUserId);
            }
        }

        /// <summary>
        /// Get user's matching data and preferences
        /// </summary>
        public async Task<object> GetUserMatchingDataAsync(string userId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var matchFynContext = scope.ServiceProvider.GetRequiredService<MatchFynDbContext>();
                var identityContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

                var user = await identityContext.Users.FindAsync(userId);
                if (user == null) return new { };

                var userInterests = await matchFynContext.UserInterests
                    .Include(ui => ui.Interest)
                    .Where(ui => ui.User.Email == user.Email)
                    .Select(ui => ui.Interest.Name)
                    .ToListAsync();

                var matchCount = await matchFynContext.Matches
                    .CountAsync(m => m.SenderId.ToString() == userId || m.ReceiverId.ToString() == userId);

                return new
                {
                    UserId = userId,
                    Age = DateTime.UtcNow.Year - user.DateOfBirth.Year,
                    Gender = user.Gender,
                    City = user.City,
                    Interests = userInterests,
                    TotalMatches = matchCount,
                    LastActive = user.LastLoginAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user matching data for {UserId}", userId);
                return new { };
            }
        }

        #region Private Helper Methods

        private static double CalculateAgeCompatibility(DateTime birthDate1, DateTime birthDate2)
        {
            var age1 = DateTime.UtcNow.Year - birthDate1.Year;
            var age2 = DateTime.UtcNow.Year - birthDate2.Year;
            
            var ageDifference = Math.Abs(age1 - age2);
            
            // Perfect score for same age, decreasing score for larger differences
            return ageDifference switch
            {
                0 => 1.0,
                <= 2 => 0.9,
                <= 5 => 0.7,
                <= 10 => 0.5,
                <= 15 => 0.3,
                _ => 0.1
            };
        }

        private async Task<double> CalculateInterestCompatibilityAsync(string email1, string email2, MatchFynDbContext context)
        {
            try
            {
                var interests1 = await context.UserInterests
                    .Where(ui => ui.User.Email == email1)
                    .Select(ui => ui.InterestId)
                    .ToListAsync();

                var interests2 = await context.UserInterests
                    .Where(ui => ui.User.Email == email2)
                    .Select(ui => ui.InterestId)
                    .ToListAsync();

                if (!interests1.Any() || !interests2.Any()) return 0.5; // Neutral score

                var commonInterests = interests1.Intersect(interests2).Count();
                var totalUniqueInterests = interests1.Union(interests2).Count();

                return totalUniqueInterests > 0 ? (double)commonInterests / totalUniqueInterests : 0.0;
            }
            catch
            {
                return 0.5; // Neutral score on error
            }
        }

        private static double CalculateLocationCompatibility(string? city1, string? city2)
        {
            if (string.IsNullOrEmpty(city1) || string.IsNullOrEmpty(city2)) return 0.5;
            
            return string.Equals(city1, city2, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.3;
        }

        private static double CalculateActivityCompatibility(DateTime? lastLogin1, DateTime? lastLogin2)
        {
            if (!lastLogin1.HasValue || !lastLogin2.HasValue) return 0.5;

            var timeDifference = Math.Abs((lastLogin1.Value - lastLogin2.Value).TotalHours);
            
            return timeDifference switch
            {
                <= 1 => 1.0,    // Both active within 1 hour
                <= 6 => 0.8,    // Within 6 hours
                <= 24 => 0.6,   // Within 24 hours
                <= 168 => 0.4,  // Within a week
                _ => 0.2         // More than a week apart
            };
        }

        #endregion
    }
}
