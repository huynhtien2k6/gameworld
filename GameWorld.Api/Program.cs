using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using GameWorld.Api.Services;
using GameWorld.Domain.Entities;
using GameWorld.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
// Only loopback proxies are trusted by default. Deployment uses Caddy on this host.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
});
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 64 * 1024);
var dataPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataPath);
var signingKey = builder.Configuration["JwtSettings:Key"];
if (string.IsNullOrWhiteSpace(signingKey))
{
    if (!builder.Environment.IsDevelopment()) throw new InvalidOperationException("Set JwtSettings__Key (at least 32 bytes) outside Development.");
    var keyPath = Path.Combine(dataPath, "jwt.key");
    if (!File.Exists(keyPath)) File.WriteAllText(keyPath, Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));
    signingKey = File.ReadAllText(keyPath).Trim();
    builder.Configuration["JwtSettings:Key"] = signingKey;
}
if (Encoding.UTF8.GetByteCount(signingKey) < 32) throw new InvalidOperationException("JWT key must contain at least 32 bytes.");
var sqlite = !string.Equals(builder.Configuration["Database:Provider"], "SqlServer", StringComparison.OrdinalIgnoreCase);
builder.Services.AddDbContext<GameDbContext>(options =>
{
    if (sqlite) options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
    else options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
});
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<WorldSimulation>();
builder.Services.AddScoped<PlayerActions>();
builder.Services.AddScoped<OperationsService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"], ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), ClockSkew = TimeSpan.FromSeconds(5)
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<GameDbContext>();
            var user = context.Principal!;
            if (user.IsInRole("Admin"))
            {
                if (!long.TryParse(user.FindFirstValue("adminId"), out var id) || !await db.AdminUsers.AnyAsync(x => x.Id == id && x.IsActive))
                    context.Fail("Admin access revoked.");
                return;
            }
            if (!long.TryParse(user.FindFirstValue("accountId"), out var accountId) || !long.TryParse(user.FindFirstValue("sessionId"), out var sessionId))
            { context.Fail("Invalid session."); return; }
            var now = DateTime.UtcNow;
            var session = await db.AccountSessions.AsNoTracking().Include(x => x.Account)
                .SingleOrDefaultAsync(x => x.Id == sessionId && x.AccountId == accountId);
            if (session is null || !session.IsActive || session.RevokedAt != null || session.ExpiresAt <= now)
            { context.Fail("Session expired or revoked."); return; }
            var activeBan = await db.AccountBans.AnyAsync(x => x.AccountId == accountId && x.LiftedAt == null &&
                (x.IsPermanent || x.BannedUntil == null || x.BannedUntil > now));
            if (activeBan || (session.Account.Status == 2 && !await db.AccountBans.AnyAsync(x => x.AccountId == accountId)))
                context.Fail("Account banned.");
        }
    };
});
builder.Services.AddAuthorization(options => options.AddPolicy("Player", policy => policy.RequireClaim("accountId").RequireRole("Player")));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => RateLimitPartition.GetFixedWindowLimiter(
        $"{context.Connection.RemoteIpAddress}:{(context.Request.Path.StartsWithSegments("/api/auth") ? "auth" : "api")}",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = context.Request.Path.StartsWithSegments("/api/auth") ? 60 : 360,
            Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
    options.OnRejected = async (context, cancellation) => await context.HttpContext.Response.WriteAsJsonAsync(
        new { message = "Bạn thao tác quá nhanh. Vui lòng thử lại sau một phút." }, cancellation);
});
var app = builder.Build();
app.UseForwardedHeaders();
await DatabaseBootstrap.InitializeAsync(app.Services, app.Configuration);
app.Use(async (context, next) =>
{
    try { await next(); }
    catch (Exception ex) when (!context.Response.HasStarted && !context.RequestAborted.IsCancellationRequested)
    {
        var status = ex switch { BadHttpRequestException badRequest => badRequest.StatusCode,
            KeyNotFoundException => 404, UnauthorizedAccessException => 403,
            ArgumentException or GameRuleException => 400, DbUpdateException => 409, SqliteException => 409, _ => 500 };
        if (status == 500)
        {
            app.Logger.LogError(ex, "Request failed: {Path}", context.Request.Path);
            try
            {
                await using var scope = app.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
                db.ErrorLogs.Add(new ErrorLog { Level = "Error", Source = context.Request.Path.Value ?? "API", Message = ex.GetType().Name + ": request failed" });
                await db.SaveChangesAsync();
            }
            catch (Exception logError) { app.Logger.LogWarning(logError, "Could not persist error log"); }
        }
        context.Response.Clear(); context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { message = status >= 500 ? "Máy chủ gặp lỗi. Vui lòng thử lại sau." :
            status == 409 ? "Dữ liệu vừa thay đổi hoặc bị trùng. Vui lòng tải lại và thử lại." : ex.Message });
    }
});
if (!app.Environment.IsDevelopment()) { app.UseHsts(); app.UseHttpsRedirection(); }
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "same-origin";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data:; style-src 'self'; script-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'";
    if (context.Request.Path.StartsWithSegments("/api")) context.Response.Headers.CacheControl = "no-store";
    await next();
});
app.UseDefaultFiles(); app.UseStaticFiles();
app.UseRateLimiter(); app.UseAuthentication(); app.UseAuthorization();
// Read and limit bodies before entering the single-writer section; slow clients cannot hold the world lock.
var writeGate = new SemaphoreSlim(1, 1);
app.Use(async (context, next) =>
{
    var writing = sqlite && context.Request.Path.StartsWithSegments("/api") &&
        !HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) && !HttpMethods.IsOptions(context.Request.Method);
    if (!writing) { await next(); return; }
    context.Request.EnableBuffering(64 * 1024, 64 * 1024);
    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
    timeout.CancelAfter(TimeSpan.FromSeconds(10));
    try { await context.Request.Body.CopyToAsync(Stream.Null, timeout.Token); context.Request.Body.Position = 0; }
    catch (OperationCanceledException) when (!context.RequestAborted.IsCancellationRequested)
    { context.Response.StatusCode = 408; await context.Response.WriteAsJsonAsync(new { message = "Yêu cầu gửi dữ liệu quá chậm." }); return; }
    catch (IOException)
    { context.Response.StatusCode = 413; await context.Response.WriteAsJsonAsync(new { message = "Yêu cầu vượt giới hạn 64 KB." }); return; }
    await writeGate.WaitAsync(context.RequestAborted);
    try { await next(); } finally { writeGate.Release(); }
});
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapGet("/health", async (GameDbContext db) => await db.Database.CanConnectAsync()
    ? Results.Ok(new { status = "healthy", utc = DateTime.UtcNow }) : Results.StatusCode(503));
app.MapControllers();
app.Run();
