using Microsoft.EntityFrameworkCore;
using SmartGym.Api.Authentication;
using SmartGym.Api.Authorization;
using SmartGym.Api.Data;
using SmartGym.Api.DTOs.Auth;
using SmartGym.Api.Entities;

namespace SmartGym.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string? refreshToken, Guid? userId, CancellationToken cancellationToken = default);
    Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public class AuthService : IAuthService
{
    private readonly SmartGymDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        SmartGymDbContext dbContext,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 1. Verify email uniqueness
        var existingUser = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (existingUser != null)
        {
            _logger.LogWarning("Registration failed: Email '{Email}' is already registered.", normalizedEmail);
            throw new InvalidOperationException("An account with this email address already exists.");
        }

        // 2. Resolve Role (Security policy: self-registration cannot grant Admin)
        var requestedRole = request.Role?.Trim();
        var targetRoleName = AppRoles.Member;

        if (!string.IsNullOrWhiteSpace(requestedRole))
        {
            if (requestedRole.Equals(AppRoles.Trainer, StringComparison.OrdinalIgnoreCase))
            {
                targetRoleName = AppRoles.Trainer;
            }
            else if (requestedRole.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Security violation: Attempted self-registration as Admin from email '{Email}'. Falling back to Member.", normalizedEmail);
                targetRoleName = AppRoles.Member;
            }
        }

        var role = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Name.ToLower() == targetRoleName.ToLower(), cancellationToken);

        if (role == null)
        {
            role = new Role
            {
                Name = targetRoleName,
                Description = $"{targetRoleName} role",
                CreatedAt = DateTime.UtcNow
            };
            await _dbContext.Roles.AddAsync(role, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // 3. Hash password with BCrypt (Cost Factor 11)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 11);

        // 4. Create User entity
        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Users.AddAsync(user, cancellationToken);

        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            AssignedAt = DateTime.UtcNow
        };
        await _dbContext.UserRoles.AddAsync(userRole, cancellationToken);

        // 5. Create Member profile if user is a member
        Member? memberProfile = null;
        if (targetRoleName == AppRoles.Member)
        {
            memberProfile = new Member
            {
                UserId = user.Id,
                EmergencyContactName = request.EmergencyContactName?.Trim(),
                EmergencyContactPhone = request.EmergencyContactPhone?.Trim(),
                Gender = request.Gender?.Trim(),
                Address = request.Address?.Trim(),
                MedicalConditions = request.MedicalConditions?.Trim(),
                JoinDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            await _dbContext.Members.AddAsync(memberProfile, cancellationToken);
        }

        // 6. Generate access token and refresh token
        var rolesList = new List<string> { targetRoleName };
        var (accessToken, expiresIn) = _tokenService.GenerateAccessToken(user, rolesList);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id);

        await _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User '{Email}' registered successfully with role '{Role}'.", user.Email, targetRoleName);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            TokenType = "Bearer",
            ExpiresIn = expiresIn,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                Roles = rolesList,
                CreatedAt = user.CreatedAt
            }
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        // Uniform error response prevents email enumeration attacks
        if (user == null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for email '{Email}'.", normalizedEmail);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        if (roles.Count == 0)
        {
            roles.Add(AppRoles.Member);
        }

        var (accessToken, expiresIn) = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id);

        await _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User '{Email}' logged in successfully.", user.Email);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            TokenType = "Bearer",
            ExpiresIn = expiresIn,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                Roles = roles,
                CreatedAt = user.CreatedAt
            }
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshTokenString, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenString))
        {
            throw new UnauthorizedAccessException("Refresh token is required.");
        }

        var existingToken = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.Token == refreshTokenString, cancellationToken);

        if (existingToken == null || existingToken.IsRevoked || existingToken.IsExpired)
        {
            _logger.LogWarning("Invalid or expired refresh token presented.");
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        var user = existingToken.User;
        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("User account is inactive.");
        }

        // Token rotation: Revoke old token and issue a fresh one
        var newRefreshToken = _tokenService.GenerateRefreshToken(user.Id);
        existingToken.IsRevoked = true;
        existingToken.ReplacedByToken = newRefreshToken.Token;

        await _dbContext.RefreshTokens.AddAsync(newRefreshToken, cancellationToken);

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var (newAccessToken, expiresIn) = _tokenService.GenerateAccessToken(user, roles);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Refreshed access token for user '{Email}'.", user.Email);

        return new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            TokenType = "Bearer",
            ExpiresIn = expiresIn,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                Roles = roles,
                CreatedAt = user.CreatedAt
            }
        };
    }

    public async Task LogoutAsync(string? refreshTokenString, Guid? userId, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(refreshTokenString))
        {
            var token = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshTokenString, cancellationToken);

            if (token != null)
            {
                token.IsRevoked = true;
            }
        }
        else if (userId.HasValue)
        {
            // Revoke all active refresh tokens for the user
            var activeTokens = await _dbContext.RefreshTokens
                .Where(rt => rt.UserId == userId.Value && !rt.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var t in activeTokens)
            {
                t.IsRevoked = true;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Logout processed for user ID '{UserId}'.", userId);
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.MemberProfile)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        MemberProfileDto? memberProfileDto = null;
        if (user.MemberProfile != null)
        {
            memberProfileDto = new MemberProfileDto
            {
                Id = user.MemberProfile.Id,
                EmergencyContactName = user.MemberProfile.EmergencyContactName,
                EmergencyContactPhone = user.MemberProfile.EmergencyContactPhone,
                Gender = user.MemberProfile.Gender,
                Address = user.MemberProfile.Address,
                MedicalConditions = user.MemberProfile.MedicalConditions,
                JoinDate = user.MemberProfile.JoinDate
            };
        }

        return new CurrentUserResponse
        {
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                Roles = roles,
                CreatedAt = user.CreatedAt
            },
            MemberProfile = memberProfileDto
        };
    }
}
