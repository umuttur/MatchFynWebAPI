using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MatchFynWebAPI.Configuration;
using MatchFynWebAPI.Models.Identity;

namespace MatchFynWebAPI.Services
{
    /// <summary>
    /// Professional JWT token service implementation
    /// Follows security best practices and clean architecture principles
    /// </summary>
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<TokenService> _logger;

        public TokenService(IOptions<JwtSettings> jwtSettings, ILogger<TokenService> logger)
        {
            _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Validate JWT settings on service initialization
            if (!_jwtSettings.IsValid())
            {
                throw new InvalidOperationException("JWT settings are not properly configured");
            }
        }

        /// <summary>
        /// Generate JWT access token with comprehensive claims
        /// </summary>
        public async Task<string> GenerateAccessTokenAsync(ApplicationUser user, IList<string> roles)
        {
            try
            {
                if (user == null)
                    throw new ArgumentNullException(nameof(user));

                if (string.IsNullOrEmpty(user.Id))
                    throw new ArgumentException("User ID cannot be null or empty", nameof(user));

                // Create comprehensive claims for the user
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.Id),
                    new(ClaimTypes.Name, user.UserName ?? string.Empty),
                    new(ClaimTypes.Email, user.Email ?? string.Empty),
                    new("FullName", user.FullName),
                    new("IsEmailVerified", user.IsEmailVerified.ToString()),
                    new("IsPhoneVerified", user.IsPhoneVerified.ToString()),
                    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
                };

                // Add role claims
                if (roles?.Any() == true)
                {
                    claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
                }

                // Add phone number if available
                if (!string.IsNullOrEmpty(user.PhoneNumber))
                {
                    claims.Add(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber));
                }

                // Create signing credentials
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                // Create JWT token
                var token = new JwtSecurityToken(
                    issuer: _jwtSettings.Issuer,
                    audience: _jwtSettings.Audience,
                    claims: claims,
                    expires: DateTime.UtcNow.Add(_jwtSettings.AccessTokenExpiry),
                    signingCredentials: credentials
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                _logger.LogInformation("JWT token generated successfully for user {UserId}", user.Id);
                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for user {UserId}", user?.Id);
                throw;
            }
        }

        /// <summary>
        /// Generate cryptographically secure refresh token
        /// </summary>
        public string GenerateRefreshToken()
        {
            try
            {
                var randomNumber = new byte[64];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(randomNumber);
                var refreshToken = Convert.ToBase64String(randomNumber);

                _logger.LogDebug("Refresh token generated successfully");
                return refreshToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating refresh token");
                throw;
            }
        }

        /// <summary>
        /// Extract claims from expired token for refresh token validation
        /// </summary>
        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return null;

                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = _jwtSettings.ValidateAudience,
                    ValidateIssuer = _jwtSettings.ValidateIssuer,
                    ValidateIssuerSigningKey = _jwtSettings.ValidateIssuerSigningKey,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
                    ValidateLifetime = false, // Don't validate lifetime for expired token
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidAudience = _jwtSettings.Audience,
                    ClockSkew = TimeSpan.Zero
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

                // Verify token algorithm
                if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogWarning("Invalid token algorithm detected");
                    return null;
                }

                _logger.LogDebug("Principal extracted from expired token successfully");
                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting principal from expired token");
                return null;
            }
        }

        /// <summary>
        /// Validate refresh token against user's stored token
        /// </summary>
        public bool ValidateRefreshToken(string refreshToken, ApplicationUser user)
        {
            try
            {
                if (string.IsNullOrEmpty(refreshToken) || user == null)
                {
                    _logger.LogWarning("Invalid refresh token or user for validation");
                    return false;
                }

                // Check if refresh token matches
                if (user.RefreshToken != refreshToken)
                {
                    _logger.LogWarning("Refresh token mismatch for user {UserId}", user.Id);
                    return false;
                }

                // Check if refresh token is expired
                if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Refresh token expired for user {UserId}", user.Id);
                    return false;
                }

                _logger.LogDebug("Refresh token validated successfully for user {UserId}", user.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating refresh token for user {UserId}", user?.Id);
                return false;
            }
        }
    }
}
