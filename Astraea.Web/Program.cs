using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Astraea.Application;
using Astraea.Application.Abstractions;
using Astraea.Application.Retention;
using Astraea.Application.Study;
using Astraea.Domain;
using Astraea.Infrastructure;
using Astraea.Infrastructure.Background;
using Astraea.Infrastructure.Persistence;
using Astraea.Web.Hubs;
using Astraea.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/astraea-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    var key = builder.Configuration["Jwt:Key"]
        ?? "Astraea-local-development-key-must-be-at-least-32-characters";

    builder.Services.AddDbContext<AstraeaDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("Astraea")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=Astraea;Trusted_Connection=True;TrustServerCertificate=True"));

    builder.Services.AddDataProtection();
    builder.Services.AddHttpClient();

    builder.Services.AddSingleton<ITokenFactory>(new JwtTokenFactory(key));
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IMentorService, MentorService>();
    builder.Services.AddScoped<ISkillService, SkillService>();
    builder.Services.AddScoped<IReportService, ReportService>();
    builder.Services.AddScoped<IReminderService, ReminderService>();
    builder.Services.AddScoped<IGitHubSyncService, GitHubSyncService>();
    builder.Services.AddScoped<IStudyLogService, StudyLogService>();
    builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();

    builder.Services.AddSingleton<IRetentionService, RetentionService>();
    builder.Services.AddSingleton<ISkillStatusPublisher, SignalRSkillStatusPublisher>();
    builder.Services.AddHostedService<NightlySyncBackgroundService>();

    builder.Services.AddSignalR();
    builder.Services
        .AddControllers()
        .AddJsonOptions(options =>
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true
            };
        });

    builder.Services.AddAuthorization();
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy
                .WithOrigins("http://localhost:5173", "https://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        await DbInitializer.InitializeAsync(scope.ServiceProvider.GetRequiredService<AstraeaDbContext>());
    }

    app.UseSerilogRequestLogging();
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<SkillStatusHub>("/hubs/skill-status");
    app.MapFallbackToFile("astraea-platform.html");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Astraea terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

sealed class JwtTokenFactory(string key) : ITokenFactory
{
    public string Create(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new("astraea_role", user.Role.ToString())
        };

        if (user.Role is UserRole.Learner or UserRole.Both)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Learner"));
        }

        if (user.Role is UserRole.Mentor or UserRole.Both)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Mentor"));
        }

        if (user.Role == UserRole.Admin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
