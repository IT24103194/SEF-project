using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartGym.Api.Configuration;
using SmartGym.Api.DTOs.Auth;
using Xunit;

namespace SmartGym.Api.Tests;

public class AuthAndSecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthAndSecurityTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsCreatedAndAuthResponse()
    {
        // Arrange
        var uniqueEmail = $"newuser_{Guid.NewGuid():N}@smartgym.com";
        var request = new RegisterRequest
        {
            Email = uniqueEmail,
            Password = "SecurePassword123!",
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "+94 77 111 2222",
            Role = "Member",
            EmergencyContactName = "Emergency Contact",
            EmergencyContactPhone = "+94 77 999 8888"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
        Assert.NotNull(authResponse);
        Assert.False(string.IsNullOrWhiteSpace(authResponse.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(authResponse.RefreshToken));
        Assert.Equal("Bearer", authResponse.TokenType);
        Assert.Equal(uniqueEmail, authResponse.User.Email);
        Assert.Contains("Member", authResponse.User.Roles);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409Conflict()
    {
        // Arrange - admin@smartgym.com is seeded in Phase 02
        var request = new RegisterRequest
        {
            Email = "admin@smartgym.com",
            Password = "AnotherPassword123!",
            FirstName = "Imposter",
            LastName = "Admin"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("already exists", content);
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "not-an-email",
            Password = "SecurePassword123!",
            FirstName = "Bad",
            LastName = "Email"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("valid email", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_InvalidPassword_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = $"weakpw_{Guid.NewGuid():N}@smartgym.com",
            Password = "weak", // Fails minimum length and complexity
            FirstName = "Weak",
            LastName = "Password"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Password", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkAndAuthResponse()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "admin@smartgym.com",
            Password = "Admin123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));
        Assert.Contains("Admin", auth.User.Roles);
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns401Unauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "admin@smartgym.com",
            Password = "WrongPassword999!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid email or password", content);
    }

    [Fact]
    public async Task Login_NonExistentUser_Returns401Unauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "doesnotexist@smartgym.com",
            Password = "SomePassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_ValidToken_ReturnsNewTokens()
    {
        // Arrange - Login to obtain refresh token
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "member@smartgym.com",
            Password = "Member123!"
        });
        var initialAuth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
        Assert.NotNull(initialAuth);

        // Act - Refresh
        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = initialAuth.RefreshToken
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refreshedAuth = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
        Assert.NotNull(refreshedAuth);
        Assert.False(string.IsNullOrWhiteSpace(refreshedAuth.AccessToken));
        Assert.NotEqual(initialAuth.RefreshToken, refreshedAuth.RefreshToken); // Token rotation
    }

    [Fact]
    public async Task RefreshToken_RevokedOrInvalidToken_Returns401Unauthorized()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = "completely_bogus_refresh_token_string"
        });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_Authenticated_ReturnsProfile()
    {
        // Arrange - Login as member
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "member@smartgym.com",
            Password = "Member123!"
        });
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
        Assert.NotNull(auth);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var currentUser = await response.Content.ReadFromJsonAsync<CurrentUserResponse>(_jsonOptions);
        Assert.NotNull(currentUser);
        Assert.Equal("member@smartgym.com", currentUser.User.Email);
        Assert.NotNull(currentUser.MemberProfile);
    }

    [Fact]
    public async Task ProtectedEndpoint_NoToken_Returns401Unauthorized()
    {
        // Act - Attempt to access protected endpoint without Authorization header
        var response = await _client.GetAsync("/api/security/authenticated");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        Assert.Equal(401, root.GetProperty("status").GetInt32());
        Assert.Equal("Unauthorized", root.GetProperty("title").GetString());
    }

    [Fact]
    public async Task ProtectedEndpoint_ExpiredToken_Returns401Unauthorized()
    {
        // Arrange - Generate an already-expired JWT token
        using var scope = _factory.Services.CreateScope();
        var jwtSettings = scope.ServiceProvider.GetRequiredService<IOptions<JwtSettings>>().Value;
        var key = Encoding.UTF8.GetBytes(jwtSettings.Key);

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Email, "expired@smartgym.com"),
                new Claim(ClaimTypes.Role, "Member")
            }),
            NotBefore = DateTime.UtcNow.AddMinutes(-30),
            Expires = DateTime.UtcNow.AddMinutes(-10), // Expired 10 minutes ago
            Issuer = jwtSettings.Issuer,
            Audience = jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var expiredToken = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/security/authenticated");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RoleAuthorization_MemberAccessingAdminEndpoint_Returns403Forbidden()
    {
        // Arrange - Authenticate as Member
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "member@smartgym.com",
            Password = "Member123!"
        });
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
        Assert.NotNull(auth);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/security/admin");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        // Act - Member attempts to access Admin endpoint
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        Assert.Equal(403, root.GetProperty("status").GetInt32());
        Assert.Equal("Forbidden", root.GetProperty("title").GetString());
    }

    [Fact]
    public async Task RoleAuthorization_TrainerAccessingAdminEndpoint_Returns403Forbidden()
    {
        // Arrange - Authenticate as Trainer
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "trainer@smartgym.com",
            Password = "Trainer123!"
        });
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
        Assert.NotNull(auth);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/security/admin");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        // Act - Trainer attempts to access Admin-only operation
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RoleAuthorization_AdminAccessingAdminEndpoint_Returns200Ok()
    {
        // Arrange - Authenticate as Admin
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "admin@smartgym.com",
            Password = "Admin123!"
        });
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
        Assert.NotNull(auth);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/security/admin");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RoleAuthorization_TrainerAccessingTrainerEndpoint_Returns200Ok()
    {
        // Arrange - Authenticate as Trainer
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "trainer@smartgym.com",
            Password = "Trainer123!"
        });
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
        Assert.NotNull(auth);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/security/trainer");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RoleAuthorization_MemberAccessingMemberEndpoint_Returns200Ok()
    {
        // Arrange - Authenticate as Member
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "member@smartgym.com",
            Password = "Member123!"
        });
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
        Assert.NotNull(auth);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/security/member");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CorrelationId_PropagatedInResponseHeader()
    {
        // Arrange
        var customCorrelationId = $"corr-{Guid.NewGuid():N}";
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Correlation-ID", customCorrelationId);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
        var returnedId = response.Headers.GetValues("X-Correlation-ID").FirstOrDefault();
        Assert.Equal(customCorrelationId, returnedId);
    }

    [Fact]
    public async Task Logout_RevokesRefreshTokenSuccessfully()
    {
        // Arrange - Login as member
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "member@smartgym.com",
            Password = "Member123!"
        });
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
        Assert.NotNull(auth);

        // Act - Call Logout
        var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout")
        {
            Content = JsonContent.Create(new LogoutRequest { RefreshToken = auth.RefreshToken })
        };
        logoutRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var logoutResponse = await _client.SendAsync(logoutRequest);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        // Assert - Attempting to use the revoked refresh token now yields 401 Unauthorized
        var refreshAttempt = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = auth.RefreshToken
        });

        Assert.Equal(HttpStatusCode.Unauthorized, refreshAttempt.StatusCode);
    }

    [Fact]
    public async Task SwaggerJson_ContainsJwtSecurityScheme()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        var securityDefinitions = root.GetProperty("components").GetProperty("securitySchemes");
        Assert.True(securityDefinitions.TryGetProperty("Bearer", out var bearerScheme));
        Assert.Equal("http", bearerScheme.GetProperty("type").GetString());
        Assert.Equal("bearer", bearerScheme.GetProperty("scheme").GetString(), ignoreCase: true);
    }
}
