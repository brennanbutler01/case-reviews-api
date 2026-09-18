using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using case_reviews_server.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var demo = builder.Configuration.GetValue<bool>("Demo:Enabled");
if (demo && !builder.Environment.IsDevelopment())
    throw new InvalidOperationException("Demo authentication is only allowed in Development.");
const string demoIssuer = "case-reviews-local-demo";
// This public key material is deliberately restricted to the isolated local demo.
const string demoKey = "case-reviews-synthetic-local-demo-key-not-for-production-2026";
builder.Services.AddControllers();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://127.0.0.1:5189"])
    .AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    if (demo)
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
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Run();
