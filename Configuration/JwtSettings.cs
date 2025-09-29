using System.ComponentModel.DataAnnotations;

namespace MatchFynWebAPI.Configuration
{
    /// <summary>
    /// JWT configuration settings
    /// Implements strongly-typed configuration following .NET best practices
    /// </summary>
    public class JwtSettings
    {
        /// <summary>
        /// Configuration section name in appsettings.json
        /// </summary>
        public const string SectionName = "JwtSettings";

        /// <summary>
        /// JWT secret key for token signing
        /// Must be at least 32 characters for security
        /// </summary>
        [Required]
        [MinLength(32, ErrorMessage = "JWT Secret must be at least 32 characters long")]
        public string Secret { get; set; } = string.Empty;

        /// <summary>
        /// Token issuer - typically your application name or domain
        /// </summary>
        [Required]
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// Token audience - typically your client application
        /// </summary>
        [Required]
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// Access token expiry time in minutes
        /// Default: 15 minutes for security
        /// </summary>
        [Range(1, 1440, ErrorMessage = "AccessTokenExpiryMinutes must be between 1 and 1440 (24 hours)")]
        public int AccessTokenExpiryMinutes { get; set; } = 15;

        /// <summary>
        /// Refresh token expiry time in days
        /// Default: 7 days
        /// </summary>
        [Range(1, 365, ErrorMessage = "RefreshTokenExpiryDays must be between 1 and 365")]
        public int RefreshTokenExpiryDays { get; set; } = 7;

        /// <summary>
        /// Whether to validate token issuer
        /// </summary>
        public bool ValidateIssuer { get; set; } = true;

        /// <summary>
        /// Whether to validate token audience
        /// </summary>
        public bool ValidateAudience { get; set; } = true;

        /// <summary>
        /// Whether to validate token lifetime
        /// </summary>
        public bool ValidateLifetime { get; set; } = true;

        /// <summary>
        /// Whether to validate issuer signing key
        /// </summary>
        public bool ValidateIssuerSigningKey { get; set; } = true;

        /// <summary>
        /// Clock skew tolerance in minutes
        /// Allows for small time differences between servers
        /// </summary>
        [Range(0, 10, ErrorMessage = "ClockSkewMinutes must be between 0 and 10")]
        public int ClockSkewMinutes { get; set; } = 5;

        /// <summary>
        /// Validate configuration settings
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Secret) &&
                   Secret.Length >= 32 &&
                   !string.IsNullOrWhiteSpace(Issuer) &&
                   !string.IsNullOrWhiteSpace(Audience) &&
                   AccessTokenExpiryMinutes > 0 &&
                   RefreshTokenExpiryDays > 0;
        }

        /// <summary>
        /// Get access token expiry as TimeSpan
        /// </summary>
        public TimeSpan AccessTokenExpiry => TimeSpan.FromMinutes(AccessTokenExpiryMinutes);

        /// <summary>
        /// Get refresh token expiry as TimeSpan
        /// </summary>
        public TimeSpan RefreshTokenExpiry => TimeSpan.FromDays(RefreshTokenExpiryDays);

        /// <summary>
        /// Get clock skew as TimeSpan
        /// </summary>
        public TimeSpan ClockSkew => TimeSpan.FromMinutes(ClockSkewMinutes);
    }
}
