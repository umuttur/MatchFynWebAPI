using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MatchFynWebAPI.DTOs.Auth;
using MatchFynWebAPI.Models.Identity;
using MatchFynWebAPI.Services;
using MatchFynWebAPI.Data;
using MatchFynWebAPI.Configuration;
using Microsoft.Extensions.Options;

namespace MatchFynWebAPI.Controllers
{
    /// <summary>
    /// Authentication controller for MatchFyn API
    /// Implements professional security practices and clean architecture
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthController> _logger;
        private readonly MatchFynDbContext _matchFynContext;
        private readonly JwtSettings _jwtSettings;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            ILogger<AuthController> logger,
            MatchFynDbContext matchFynContext,
            IOptions<JwtSettings> jwtSettings)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _matchFynContext = matchFynContext ?? throw new ArgumentNullException(nameof(matchFynContext));
            _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto registerDto)
        {
            try
            {
                _logger.LogInformation("Registration attempt for email: {Email}", registerDto.Email);

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("Registration failed - email already exists: {Email}", registerDto.Email);
                    return BadRequest(new { message = "Email already exists" });
                }

                // Validate age (must be 18+)
                var age = DateTime.UtcNow.Year - registerDto.DateOfBirth.Year;
                if (registerDto.DateOfBirth > DateTime.UtcNow.AddYears(-age)) age--;
                
                if (age < 18)
                {
                    return BadRequest(new { message = "You must be at least 18 years old to register" });
                }

                // Create new user
                var user = new ApplicationUser
                {
                    UserName = registerDto.Email,
                    Email = registerDto.Email,
                    FullName = registerDto.FullName,
                    PhoneNumber = registerDto.PhoneNumber,
                    DateOfBirth = registerDto.DateOfBirth,
                    Gender = registerDto.Gender,
                    City = registerDto.City,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, registerDto.Password);
                if (!result.Succeeded)
                {
                    _logger.LogError("User creation failed for {Email}: {Errors}", 
                        registerDto.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                    return BadRequest(new { message = "User creation failed", errors = result.Errors });
                }

                // Add user to default role
                await _userManager.AddToRoleAsync(user, ApplicationRoles.User);

                // Create corresponding user in main database for interests
                if (registerDto.InterestIds.Any())
                {
                    var mainUser = new Models.User
                    {
                        Name = registerDto.FullName,
                        Email = registerDto.Email,
                        PhoneNumber = registerDto.PhoneNumber,
                        DateOfBirth = registerDto.DateOfBirth,
                        Gender = registerDto.Gender,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    _matchFynContext.Users.Add(mainUser);
                    await _matchFynContext.SaveChangesAsync();

                    // Add user interests
                    var userInterests = registerDto.InterestIds.Select(interestId => new Models.UserInterest
                    {
                        UserId = mainUser.Id,
                        InterestId = interestId
                    }).ToList();

                    _matchFynContext.UserInterests.AddRange(userInterests);
                    await _matchFynContext.SaveChangesAsync();
                }

                // Generate email confirmation token
                var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                // TODO: Send email confirmation (implement email service)

                _logger.LogInformation("User registered successfully: {UserId}", user.Id);

                // Generate tokens
                var roles = await _userManager.GetRolesAsync(user);
                var accessToken = await _tokenService.GenerateAccessTokenAsync(user, roles);
                var refreshToken = _tokenService.GenerateRefreshToken();

                // Save refresh token
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.Add(_jwtSettings.RefreshTokenExpiry);
                await _userManager.UpdateAsync(user);

                var response = new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.Add(_jwtSettings.AccessTokenExpiry),
                    User = new UserProfileDto
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        DateOfBirth = user.DateOfBirth,
                        Gender = user.Gender,
                        City = user.City,
                        ProfileImageUrl = user.ProfileImageUrl,
                        Bio = user.Bio,
                        IsEmailVerified = user.IsEmailVerified,
                        IsPhoneVerified = user.IsPhoneVerified,
                        Roles = roles.ToList()
                    }
                };

                return CreatedAtAction(nameof(GetProfile), new { id = user.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for email: {Email}", registerDto.Email);
                return StatusCode(500, new { message = "Internal server error during registration" });
            }
        }

        /// <summary>
        /// Login user
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
        {
            try
            {
                _logger.LogInformation("Login attempt for email: {Email}", loginDto.Email);

                var user = await _userManager.FindByEmailAsync(loginDto.Email);
                if (user == null || !user.IsActive)
                {
                    _logger.LogWarning("Login failed - user not found or inactive: {Email}", loginDto.Email);
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Login failed for user: {Email}. Reason: {Reason}", 
                        loginDto.Email, result.IsLockedOut ? "Locked out" : "Invalid password");
                    
                    if (result.IsLockedOut)
                        return Unauthorized(new { message = "Account is locked due to multiple failed attempts" });
                    
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // Generate tokens
                var roles = await _userManager.GetRolesAsync(user);
                var accessToken = await _tokenService.GenerateAccessTokenAsync(user, roles);
                var refreshToken = _tokenService.GenerateRefreshToken();

                // Save refresh token
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.Add(_jwtSettings.RefreshTokenExpiry);
                await _userManager.UpdateAsync(user);

                // Get user interests from main database
                var interests = await _matchFynContext.UserInterests
                    .Where(ui => ui.User.Email == user.Email)
                    .Select(ui => ui.Interest.Name)
                    .ToListAsync();

                var response = new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.Add(_jwtSettings.AccessTokenExpiry),
                    User = new UserProfileDto
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        DateOfBirth = user.DateOfBirth,
                        Gender = user.Gender,
                        City = user.City,
                        ProfileImageUrl = user.ProfileImageUrl,
                        Bio = user.Bio,
                        IsEmailVerified = user.IsEmailVerified,
                        IsPhoneVerified = user.IsPhoneVerified,
                        Roles = roles.ToList(),
                        Interests = interests
                    }
                };

                _logger.LogInformation("User logged in successfully: {UserId}", user.Id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", loginDto.Email);
                return StatusCode(500, new { message = "Internal server error during login" });
            }
        }

        /// <summary>
        /// Refresh access token
        /// </summary>
        [HttpPost("refresh-token")]
        public async Task<ActionResult<AuthResponseDto>> RefreshToken(RefreshTokenDto refreshTokenDto)
        {
            try
            {
                var principal = _tokenService.GetPrincipalFromExpiredToken(refreshTokenDto.AccessToken);
                if (principal == null)
                {
                    return Unauthorized(new { message = "Invalid access token" });
                }

                var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token claims" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null || !user.IsActive)
                {
                    return Unauthorized(new { message = "User not found or inactive" });
                }

                if (!_tokenService.ValidateRefreshToken(refreshTokenDto.RefreshToken, user))
                {
                    return Unauthorized(new { message = "Invalid refresh token" });
                }

                // Generate new tokens
                var roles = await _userManager.GetRolesAsync(user);
                var newAccessToken = await _tokenService.GenerateAccessTokenAsync(user, roles);
                var newRefreshToken = _tokenService.GenerateRefreshToken();

                // Update refresh token
                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.Add(_jwtSettings.RefreshTokenExpiry);
                await _userManager.UpdateAsync(user);

                var response = new AuthResponseDto
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = DateTime.UtcNow.Add(_jwtSettings.AccessTokenExpiry),
                    User = new UserProfileDto
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        Email = user.Email,
                        Roles = roles.ToList()
                    }
                };

                _logger.LogInformation("Token refreshed successfully for user: {UserId}", user.Id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new { message = "Internal server error during token refresh" });
            }
        }

        /// <summary>
        /// Logout user (revoke refresh token)
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { message = "Invalid user" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    // Revoke refresh token
                    user.RefreshToken = null;
                    user.RefreshTokenExpiryTime = null;
                    await _userManager.UpdateAsync(user);
                }

                _logger.LogInformation("User logged out successfully: {UserId}", userId);
                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { message = "Internal server error during logout" });
            }
        }

        /// <summary>
        /// Get current user profile
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserProfileDto>> GetProfile()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { message = "Invalid user" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null || !user.IsActive)
                {
                    return NotFound(new { message = "User not found" });
                }

                var roles = await _userManager.GetRolesAsync(user);
                var interests = await _matchFynContext.UserInterests
                    .Where(ui => ui.User.Email == user.Email)
                    .Select(ui => ui.Interest.Name)
                    .ToListAsync();

                var profile = new UserProfileDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    DateOfBirth = user.DateOfBirth,
                    Gender = user.Gender,
                    City = user.City,
                    ProfileImageUrl = user.ProfileImageUrl,
                    Bio = user.Bio,
                    IsEmailVerified = user.IsEmailVerified,
                    IsPhoneVerified = user.IsPhoneVerified,
                    Roles = roles.ToList(),
                    Interests = interests
                };

                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
