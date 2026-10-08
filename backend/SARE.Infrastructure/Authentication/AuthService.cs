using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SARE.Application.Common.Exceptions;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Auth;
using SARE.Domain.Users;
using SARE.Infrastructure.Persistence;

namespace SARE.Infrastructure.Authentication;

public sealed class AuthService(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    IJwtTokenGenerator tokenGenerator,
    AppDbContext dbContext,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new AuthException("User with this email already exists.");
        }

        if (!string.IsNullOrWhiteSpace(request.NfcUid))
        {
            var nfcTaken = await dbContext.Users.AnyAsync(u => u.NfcUid == request.NfcUid, cancellationToken);
            if (nfcTaken)
            {
                throw new AuthException("User with this NFC UID already exists.");
            }
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            UserName = request.Email.Trim().ToLowerInvariant(),
            NfcUid = string.IsNullOrWhiteSpace(request.NfcUid) ? null : request.NfcUid.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new AuthException($"Registration failed: {errors}");
        }

        if (await roleManager.RoleExistsAsync(UserRoles.Customer))
        {
            await userManager.AddToRoleAsync(user, UserRoles.Customer);
        }

        var roles = await userManager.GetRolesAsync(user);
        return await CreateAuthResponseAsync(user, roles, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user == null || !user.IsActive)
        {
            throw new AuthException("Invalid credentials.");
        }

        var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            throw new AuthException("Invalid credentials.");
        }

        var roles = await userManager.GetRolesAsync(user);
        return await CreateAuthResponseAsync(user, roles, cancellationToken);
    }

    public async Task<AuthResponse> LoginWithNfcAsync(NfcLoginRequest request, CancellationToken cancellationToken = default)
    {
        var trimmedNfc = request.NfcUid.Trim();
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.NfcUid == trimmedNfc, cancellationToken);
        if (user == null || !user.IsActive)
        {
            throw new AuthException("Invalid NFC credentials or user is inactive.");
        }

        var roles = await userManager.GetRolesAsync(user);
        return await CreateAuthResponseAsync(user, roles, cancellationToken);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var principal = GetPrincipalFromExpiredToken(request.AccessToken);
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new AuthException("Invalid access token.");
        }

        var storedRefreshToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken && t.UserId == userId, cancellationToken);

        if (storedRefreshToken == null || !storedRefreshToken.IsActive)
        {
            throw new AuthException("Invalid or expired refresh token.");
        }

        storedRefreshToken.RevokedAtUtc = DateTime.UtcNow;

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null || !user.IsActive)
        {
            throw new AuthException("User no longer exists or is inactive.");
        }

        var roles = await userManager.GetRolesAsync(user);
        return await CreateAuthResponseAsync(user, roles, cancellationToken);
    }

    public async Task RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken, cancellationToken);
        if (token != null && !token.IsRevoked)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new AuthException("User not found.");
        }

        var roles = await userManager.GetRolesAsync(user);
        return new UserDto(
            user.Id,
            user.Name,
            user.Email ?? string.Empty,
            user.NfcUid,
            user.IsActive,
            roles.ToList(),
            user.CreatedAt);
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(
        User user,
        IEnumerable<string> roles,
        CancellationToken cancellationToken)
    {
        var roleList = roles.ToList();
        var (accessToken, expiresAtUtc) = tokenGenerator.GenerateAccessToken(user, roleList);
        var (refreshTokenString, refreshExpiresAtUtc) = tokenGenerator.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenString,
            ExpiresAtUtc = refreshExpiresAtUtc,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.RefreshTokens.Add(refreshTokenEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            user.Id,
            user.Name,
            user.Email ?? string.Empty,
            roleList,
            accessToken,
            refreshTokenString,
            expiresAtUtc);
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = !string.IsNullOrWhiteSpace(_jwtOptions.Audience),
            ValidAudience = _jwtOptions.Audience,
            ValidateIssuer = !string.IsNullOrWhiteSpace(_jwtOptions.Issuer),
            ValidIssuer = _jwtOptions.Issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey)),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new AuthException("Invalid token signature algorithm.");
        }

        return principal;
    }
}
