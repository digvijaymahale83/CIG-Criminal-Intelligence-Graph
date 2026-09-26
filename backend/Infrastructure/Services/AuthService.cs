using Application.Common.Interfaces;
using Application.DTOs;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IAppDbContext _dbContext;
    private readonly JwtTokenService _jwtService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IAppDbContext dbContext,
        JwtTokenService jwtService,
        IAuditService auditService,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _jwtService = jwtService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail && u.IsActive, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Failed login attempt for user: {Email}", request.Email);
            return null;
        }

        var isValidPassword = PasswordHasher.VerifyPassword(user.PasswordHash, request.Password);
        if (!isValidPassword)
        {
            _logger.LogWarning("Invalid password attempt for user: {Email}", request.Email);
            return null;
        }

        var (token, expiresAt) = _jwtService.GenerateToken(user);

        await _auditService.LogAsync(
            user.Id,
            user.FullName,
            "LOGIN",
            "user",
            user.Id,
            $"Investigator authenticated via CORTEX: {user.Email}",
            null,
            ipAddress,
            cancellationToken);

        return new AuthResponseDto
        {
            Token = token,
            ExpiresAtUtc = expiresAt,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                BadgeNumber = user.BadgeNumber,
                Agency = user.Agency,
                Rank = user.Rank,
                Unit = user.Unit
            }
        };
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);

        if (user == null) return null;

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            BadgeNumber = user.BadgeNumber,
            Agency = user.Agency,
            Rank = user.Rank,
            Unit = user.Unit
        };
    }
}
