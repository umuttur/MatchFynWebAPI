using MatchFynWebAPI.Models.Identity;
using System.Security.Claims;

namespace MatchFynWebAPI.Services
{
    /// <summary>
    /// Interface for JWT token service
    /// Follows Interface Segregation Principle for clean architecture
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Generate JWT access token for authenticated user
        /// </summary>
        /// <param name="user">Application user</param>
        /// <param name="roles">User roles</param>
        /// <returns>JWT token string</returns>
        Task<string> GenerateAccessTokenAsync(ApplicationUser user, IList<string> roles);

        /// <summary>
        /// Generate refresh token for token renewal
        /// </summary>
        /// <returns>Refresh token string</returns>
        string GenerateRefreshToken();

        /// <summary>
        /// Get principal from expired token for refresh token validation
        /// </summary>
        /// <param name="token">Expired JWT token</param>
        /// <returns>Claims principal</returns>
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);

        /// <summary>
        /// Validate refresh token
        /// </summary>
        /// <param name="refreshToken">Refresh token to validate</param>
        /// <param name="user">User associated with the token</param>
        /// <returns>True if valid, false otherwise</returns>
        bool ValidateRefreshToken(string refreshToken, ApplicationUser user);
    }
}
