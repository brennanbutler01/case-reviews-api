using case_reviews_server;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using case_reviews_server.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var visitorDemo = builder.Configuration.GetValue<bool>("VisitorDemo:Enabled");
var visitorKey = builder.Configuration["VisitorDemo:SigningKey"];
if (visitorDemo && (string.IsNullOrWhiteSpace(visitorKey) || Encoding.UTF8.GetByteCount(visitorKey) < 32))
    throw new InvalidOperationException("VisitorDemo__SigningKey must contain at least 32 bytes of random secret material.");
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 131072);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    if (visitorDemo)
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(context.User.FindFirst("sub")?.Value ?? "anonymous",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddFixedWindowLimiter("sessions", limiter =>
    {
        limiter.PermitLimit = 30; limiter.Window = TimeSpan.FromMinutes(1); limiter.QueueLimit = 0;
    });
});
if (visitorDemo) builder.Services.AddHostedService<VisitorCleanup>();
var demo = builder.Configuration.GetValue<bool>("Demo:Enabled");
if (demo && !builder.Environment.IsDevelopment())
    throw new InvalidOperationException("Demo authentication is only allowed in Development.");
if (demo && visitorDemo) throw new InvalidOperationException("Choose one demo mode.");
const string demoIssuer = "case-reviews-local-demo";
// This public key material is deliberately restricted to the isolated local demo.
const string demoKey = "case-reviews-synthetic-local-demo-key-not-for-production-2026";
builder.Services.AddControllers(options => { if (visitorDemo) options.Filters.Add<VisitorWriteFilter>(); });
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://127.0.0.1:5189"])
    .AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    if (visitorDemo)
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = VisitorDemo.Issuer,
            ValidateAudience = true, ValidAudience = VisitorDemo.Issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(visitorKey!)),
            ValidateLifetime = true, ClockSkew = TimeSpan.Zero, NameClaimType = "sub"
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var subject = context.Principal?.FindFirst("sub")?.Value;
                var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                if (!await db.VisitorSessions.AnyAsync(s => s.Id == subject && s.ExpiresAt > DateTime.UtcNow))
                    context.Fail("Visitor session expired or revoked.");
            }
        };
    }
    else if (demo)
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = demoIssuer,
            ValidateAudience = true, ValidAudience = demoIssuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(demoKey)),
            ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(5), NameClaimType = "sub"
        };
    }
    else
    {
        options.Authority = builder.Configuration["Auth:Authority"]
            ?? throw new InvalidOperationException("Auth:Authority is required.");
        options.Audience = builder.Configuration["Auth:Audience"]
            ?? throw new InvalidOperationException("Auth:Audience is required.");
        options.TokenValidationParameters.NameClaimType = "sub";
    }
});
builder.Services.AddAuthorization(options => options.DefaultPolicy =
    new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser().RequireClaim("sub").Build());
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("ConnectionString")
        ?? throw new InvalidOperationException("ConnectionStrings:ConnectionString is required.")));
var app = builder.Build();
if (demo)
{
    using var scope = app.Services.CreateScope();
    var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await database.Database.EnsureCreatedAsync();
    app.MapGet("/dev/token/{user}", (string user) =>
    {
        if (user is not ("alice" or "bob")) return Results.NotFound();
        var token = new JwtSecurityToken(demoIssuer, demoIssuer,
            [new Claim("sub", $"demo-{user}")], expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(demoKey)), SecurityAlgorithms.HmacSha256));
        return Results.Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(token) });
    });
}
if (visitorDemo)
{
    using var scope = app.Services.CreateScope();
    var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await database.Database.OpenConnectionAsync();
    try
    {
        // Serialize schema initialization across simultaneous container cold starts.
        await database.Database.ExecuteSqlRawAsync("SELECT pg_advisory_lock(52105210)");
        await database.Database.EnsureCreatedAsync();
    }
    finally
    {
        await database.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock(52105210)");
        await database.Database.CloseConnectionAsync();
    }
    app.MapVisitorDemo(visitorKey!);
}
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Run();
