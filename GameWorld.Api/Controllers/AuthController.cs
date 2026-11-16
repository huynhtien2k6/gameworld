using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GameWorld.Api.Services;
using GameWorld.Domain.Entities;
using GameWorld.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameWorld.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(GameDbContext db, AuthService auth) : ControllerBase
{
    private static readonly SemaphoreSlim RegistrationGate = new(1, 1);
    private readonly PasswordHasher<Account> _playerHasher = new();
    private readonly PasswordHasher<AdminUser> _adminHasher = new();

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        var email = request.Email.Trim().ToLowerInvariant();
        await RegistrationGate.WaitAsync(HttpContext.RequestAborted);
        try
        {
            if (await db.Accounts.AnyAsync(a => a.Username.ToLower() == username || a.Email.ToLower() == email))
                return Conflict(new { message = "Tên đăng nhập hoặc email đã được sử dụng." });
            var account = new Account
            {
                Username = username, Email = email, SecurityStamp = Guid.NewGuid().ToString("N"),
                IsVerified = false,
                PlayerProfile = new PlayerProfile
                {
                    DisplayName = request.Username.Trim(), Gold = 100, RegionId = 1, CurrentMapId = 1
                }
            };
            account.PasswordHash = _playerHasher.HashPassword(account, request.Password);
            db.Accounts.Add(account);
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Không thể tạo tài khoản. Tên đăng nhập hoặc email có thể đã được sử dụng." });
            }
            return Ok(new { message = "Đăng ký thành công. Bạn có thể đăng nhập.", accountId = account.Id });
        }
        finally { RegistrationGate.Release(); }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        if (await TooManyAttempts(username))
            return StatusCode(429, new { message = "Bạn đăng nhập sai quá nhiều lần. Vui lòng thử lại sau 5 phút." });
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Username.ToLower() == username);
        if (account is null || !VerifyPlayerPassword(account, request.Password))
        {
            await LogAttempt(username, account?.Id, false);
            return Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không đúng." });
        }
        if (await IsBanned(account))
        {
            await LogAttempt(username, account.Id, false, "Banned account");
            return StatusCode(403, new { message = "Tài khoản đang bị khóa. Vui lòng liên hệ hỗ trợ." });
        }
        var refreshToken = AuthService.NewRefreshToken();
        var userAgent = Request.Headers.UserAgent.ToString();
        var session = new AccountSession
        {
            AccountId = account.Id, RefreshTokenHash = AuthService.HashRefreshToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7), LastSeenAt = DateTime.UtcNow,
            IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = userAgent[..Math.Min(500, userAgent.Length)]
        };
        account.LastLoginAt = account.UpdatedAt = DateTime.UtcNow;
        db.AccountSessions.Add(session);
        db.AccountLoginAttempts.Add(NewAttempt(username, account.Id, true));
        await db.SaveChangesAsync();
        return Ok(PlayerSession(account, session, refreshToken));
    }

    [HttpPost("admin/login")]
    public async Task<IActionResult> AdminLogin(LoginRequest request)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        var auditUsername = "admin:" + username;
        if (await TooManyAttempts(auditUsername))
            return StatusCode(429, new { message = "Vui lòng thử đăng nhập quản trị sau 5 phút." });
        var admin = await db.AdminUsers.FirstOrDefaultAsync(a => a.Username.ToLower() == username);
        var result = PasswordVerificationResult.Failed;
        if (admin is not null)
        {
            try { result = _adminHasher.VerifyHashedPassword(admin, admin.PasswordHash, request.Password); }
            catch (FormatException) { }
        }
        if (admin is null || !admin.IsActive || result == PasswordVerificationResult.Failed)
        {
            await LogAttempt(auditUsername, null, false);
            return Unauthorized(new { message = "Thông tin đăng nhập quản trị không đúng." });
        }
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            admin.PasswordHash = _adminHasher.HashPassword(admin, request.Password);
        db.AccountLoginAttempts.Add(NewAttempt(auditUsername, null, true));
        await db.SaveChangesAsync();
        return Ok(new
        {
            token = auth.CreateAdminToken(admin), expiresAt = DateTime.UtcNow.AddMinutes(auth.AccessTokenMinutes),
            adminId = admin.Id, username = admin.Username, role = "Admin"
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request)
    {
        var oldHash = AuthService.HashRefreshToken(request.RefreshToken);
        var session = await db.AccountSessions.AsNoTracking().Include(s => s.Account)
            .FirstOrDefaultAsync(s => s.RefreshTokenHash == oldHash);
        var now = DateTime.UtcNow;
        if (session is null || !session.IsActive || session.RevokedAt is not null || session.ExpiresAt <= now
            || await IsBanned(session.Account))
            return Unauthorized(new { message = "Phiên đăng nhập đã hết hạn hoặc bị thu hồi." });
        var refreshToken = AuthService.NewRefreshToken();
        var newHash = AuthService.HashRefreshToken(refreshToken);
        // Compare-and-swap ensures a refresh token succeeds once, even with concurrent requests.
        var changed = await db.AccountSessions.Where(s => s.Id == session.Id && s.RefreshTokenHash == oldHash
                && s.IsActive && s.RevokedAt == null && s.ExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.RefreshTokenHash, newHash)
                .SetProperty(s => s.LastSeenAt, now));
        if (changed != 1)
            return Unauthorized(new { message = "Refresh token đã được sử dụng hoặc thu hồi." });
        session.RefreshTokenHash = newHash;
        await db.SaveChangesAsync();
        return Ok(PlayerSession(session.Account, session, refreshToken));
    }

    [Authorize(Roles = "Player")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (!long.TryParse(User.FindFirstValue("sessionId"), out var sessionId)) return Unauthorized();
        var session = await db.AccountSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session is null) return Unauthorized();
        session.IsActive = false;
        session.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { message = "Đã đăng xuất và thu hồi phiên đăng nhập." });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        if (User.IsInRole("Admin"))
        {
            if (!long.TryParse(User.FindFirstValue("adminId"), out var adminId)) return Unauthorized();
            return Ok(await db.AdminUsers.Where(a => a.Id == adminId)
                .Select(a => new { adminId = a.Id, a.Username, role = "Admin" }).SingleAsync());
        }
        if (!long.TryParse(User.FindFirstValue("accountId"), out var accountId)) return Unauthorized();
        return Ok(await db.Accounts.Where(a => a.Id == accountId).Select(a => new
        {
            accountId = a.Id, a.Username, a.Email, role = "Player", a.CreatedAt,
            profile = a.PlayerProfile == null ? null : new
            {
                a.PlayerProfile.DisplayName, a.PlayerProfile.Gold, a.PlayerProfile.Gem,
                a.PlayerProfile.Stamina, a.PlayerProfile.RegionId, a.PlayerProfile.CurrentMapId
            }
        }).SingleAsync());
    }

    [Authorize(Roles = "Player")]
    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat()
    {
        if (!long.TryParse(User.FindFirstValue("sessionId"), out var sessionId)) return Unauthorized();
        var now = DateTime.UtcNow;
        var updated = await db.AccountSessions.Where(s => s.Id == sessionId && s.IsActive && s.ExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.LastSeenAt, now));
        return updated == 1 ? Ok(new { serverTime = now }) : Unauthorized();
    }

    private object PlayerSession(Account account, AccountSession session, string refreshToken) => new
    {
        token = auth.CreateToken(account, session), refreshToken,
        expiresAt = DateTime.UtcNow.AddMinutes(auth.AccessTokenMinutes),
        refreshExpiresAt = session.ExpiresAt, accountId = account.Id, username = account.Username, role = "Player"
    };

    private bool VerifyPlayerPassword(Account account, string password)
    {
        if (!string.IsNullOrEmpty(account.PasswordSalt))
        {
            var legacyHash = SHA256.HashData(Encoding.UTF8.GetBytes(password + account.PasswordSalt));
            byte[] stored;
            try { stored = Convert.FromBase64String(account.PasswordHash); }
            catch (FormatException) { return false; }
            if (!CryptographicOperations.FixedTimeEquals(legacyHash, stored)) return false;
            account.PasswordHash = _playerHasher.HashPassword(account, password);
            account.PasswordSalt = null;
            return true;
        }
        PasswordVerificationResult result;
        try { result = _playerHasher.VerifyHashedPassword(account, account.PasswordHash, password); }
        catch (FormatException) { return false; }
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            account.PasswordHash = _playerHasher.HashPassword(account, password);
        return result != PasswordVerificationResult.Failed;
    }

    private async Task<bool> IsBanned(Account account)
    {
        var now = DateTime.UtcNow;
        if (await db.AccountBans.AnyAsync(b => b.AccountId == account.Id && b.LiftedAt == null
                && (b.IsPermanent || b.BannedUntil == null || b.BannedUntil > now))) return true;
        if (account.Status != 2) return false;
        if (!await db.AccountBans.AnyAsync(b => b.AccountId == account.Id)) return true;
        account.Status = 0;
        if (db.Entry(account).State == EntityState.Detached) db.Accounts.Attach(account);
        db.Entry(account).Property(a => a.Status).IsModified = true;
        return false;
    }

    private async Task<bool> TooManyAttempts(string username)
    {
        var since = DateTime.UtcNow.AddMinutes(-5);
        return await db.AccountLoginAttempts.CountAsync(a => a.Username == username && a.Result == 0 && a.AttemptAt > since) >= 10;
    }

    private async Task LogAttempt(string username, long? accountId, bool success, string? reason = null)
    {
        db.AccountLoginAttempts.Add(NewAttempt(username, accountId, success, reason));
        await db.SaveChangesAsync();
    }

    private AccountLoginAttempt NewAttempt(string username, long? accountId, bool success, string? reason = null) => new()
    {
        Username = username, AccountId = accountId, Result = success ? 1 : 0,
        Reason = reason ?? (success ? "Success" : "Invalid credentials"),
        IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
    };
}

public class RegisterRequest
{
    [Required, StringLength(50, MinimumLength = 3), RegularExpression(@"[a-zA-Z0-9_]{3,50}")]
    public string Username { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    [Required, StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;
    [Required, MaxLength(128)]
    public string Password { get; set; } = string.Empty;
}

public class RefreshRequest
{
    [Required, StringLength(256, MinimumLength = 32)]
    public string RefreshToken { get; set; } = string.Empty;
}
