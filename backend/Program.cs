using System.Text;
using System.Text.Json;
using backend.Data;
using backend.DTO.HealthCheck;
using backend.Hubs;
using backend.Utilities;
using backend.Utilities.Interface;
using backend.Models.Entities;
using backend.Options;
using backend.Queue;
using backend.Repositories;
using backend.Repositories.Interfaces;
using backend.Services;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;

var logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    var services = builder.Services;

    #region Add services

    services.AddControllers()
        .AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        });
    services.AddSignalR();
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "Marketplace Demo API", Version = "v1" });
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                },
                Array.Empty<string>()
            },
        });
    });

    #region appsettings.json

    var appSettings = new AppSettings();
    builder.Configuration.GetSection(AppSettings.SectionName).Bind(appSettings);
    services.AddTransient<AppSettings>(_ => appSettings);

    #endregion

    #region Database

    services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    #endregion

    #region Health check

    services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>(name: "Database", tags: new[] { "ready" })
        .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

    #endregion

    #region Json

    #endregion

    #region Dependency Injection

    services.AddHttpContextAccessor();
    services.AddScoped<ITrace, Trace>();

    #region Services

    services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
    services.AddTransient<IJwtService, JwtService>();
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<IProductService, ProductService>();
    services.AddScoped<IOrderService, OrderService>();

    #endregion

    #region Repositories

    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IRoleRepository, RoleRepository>();
    services.AddScoped<IProductRepository, ProductRepository>();
    services.AddScoped<IOrderRepository, OrderRepository>();

    #endregion

    services.AddSingleton<InMemoryOrderPlacedQueue>();
    services.AddSingleton<IOrderPlacedQueue>(sp => sp.GetRequiredService<InMemoryOrderPlacedQueue>());
    services.AddHostedService<OrderPlacedConsumer>();

    #endregion

    #region Storage

    #endregion

    #region Size Data Request

    #endregion

    #region JWT

    services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

    var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
        ?? throw new InvalidOperationException("Jwt configuration is missing.");
    if (string.IsNullOrWhiteSpace(appSettings.Jwt.Secret))
    {
        throw new InvalidOperationException("AppSettings:Jwt:Secret is required (same signing key as JwtService).");
    }

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appSettings.Jwt.Secret)),
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) &&
                        path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                },
            };
        });

    services.AddAuthorization();

    #endregion

    #endregion

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    services.AddCors(options =>
    {
        var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();
        options.AddPolicy(
            "Frontend",
            policy =>
            {
                policy.WithOrigins(corsOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
    });

    var app = builder.Build();

    await DbSeeder.SeedAsync(app.Services);

    #region Static Files and Middleware

    #endregion

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors("Frontend");
    app.UseExceptionHandler("/error");
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<MarketplaceHub>(MarketplaceHub.Path);

    var healthJson = new HealthCheckOptions
    {
        ResponseWriter = JsonHealthCheckResponseWriter,
        ResultStatusCodes =
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status200OK,
        },
    };

    app.MapHealthChecks("/health", healthJson);
    app.MapHealthChecks(
        "/health/live",
        new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live"),
            ResponseWriter = healthJson.ResponseWriter,
            ResultStatusCodes = healthJson.ResultStatusCodes,
        });
    app.MapHealthChecks(
        "/health/ready",
        new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready"),
            ResponseWriter = healthJson.ResponseWriter,
            ResultStatusCodes = healthJson.ResultStatusCodes,
        });

    app.Run();
}
catch (Exception exception)
{
    logger.Error(exception, "Application stopped because of an exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}

static Task JsonHealthCheckResponseWriter(HttpContext context, HealthReport result)
{
    var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
    context.Response.ContentType = "application/json; charset=utf-8";

    var serverStatus = new ServerStatus
    {
        EnvironmentName = env.EnvironmentName,
        Server = new ComponentStatus
        {
            Name = "Marketplace Demo API",
            Status = Enum.GetName(result.Status),
            Type = Enum.GetName(typeof(ComponentType), ComponentType.Server),
        },
    };

    foreach (var entry in result.Entries)
    {
        serverStatus.Dependencies.Add(new ComponentStatus
        {
            Name = entry.Key,
            Status = Enum.GetName(entry.Value.Status),
            Type = Enum.GetName(typeof(ComponentType), ComponentType.Component),
        });
    }

    var json = JsonSerializer.Serialize(serverStatus);
    return context.Response.WriteAsync(json);
}
