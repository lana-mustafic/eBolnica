using eBolnica.Infrastructure.Common;
using eBolnica.Shared.Dtos;
using eBolnica.Shared.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using eBolnica.Domain.Entities.Identity;
using System.Text;
using System.Threading.RateLimiting;

namespace eBolnica.API;

public static class DependencyInjection
{
    public static IServiceCollection AddAPI(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment env)
    {
        // Controllers + uniform BadRequest
        services.AddControllers()
            .ConfigureApiBehaviorOptions(opts =>
            {
                opts.InvalidModelStateResponseFactory = ctx =>
                {
                    var msg = string.Join("; ",
                        ctx.ModelState.Values.SelectMany(v => v.Errors)
                                             .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage)
                                                 ? "Validation error"
                                                 : e.ErrorMessage));
                    return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new ErrorDto
                    {
                        Code = "validation.failed",
                        Message = msg
                    });
                };
            });

        // Typed options + validation on startup
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // JWT auth (reads from IOptions<JwtOptions>)
        services.AddAuthentication(o =>
        {
            o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer((o) =>
        {
            var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;

            o.TokenValidationParameters = new()
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization(o =>
        {
            // Fallback policy requires authenticated user
            o.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // AdminOnly policy
            o.AddPolicy("AdminOnly", policy => policy.RequireRole(UserTypes.Admin));

            // Staff policy (clinical + admin)
            o.AddPolicy("Staff", policy => policy.RequireRole(
                UserTypes.Admin, UserTypes.Doctor, UserTypes.Pharmacist));

            o.AddPolicy("DoctorOnly", policy => policy.RequireRole(UserTypes.Doctor));

            o.AddPolicy("PatientOnly", policy => policy.RequireRole(UserTypes.Patient));

            o.AddPolicy("PharmacistOnly", policy => policy.RequireRole(UserTypes.Pharmacist));

            o.AddPolicy("PharmacyStaff", policy => policy.RequireRole(UserTypes.Pharmacist, UserTypes.Admin));
        });

        // Swagger with Bearer auth
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "eBolnica API", Version = "v1" });
            var xml = Path.Combine(AppContext.BaseDirectory, "eBolnica.API.xml");
            if (File.Exists(xml))
                c.IncludeXmlComments(xml, includeControllerXmlComments: true);

            var bearer = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Unesi JWT token. Format: **Bearer {token}**",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            };
            c.AddSecurityDefinition("Bearer", bearer);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement { { bearer, Array.Empty<string>() } });
        });

        services.AddExceptionHandler<eBolnicaExceptionHandler>();
        services.AddProblemDetails();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsJsonAsync(new ErrorDto
                {
                    Code = "rate_limit.exceeded",
                    Message = "Too many upload requests. Please try again later."
                }, token);
            };

            options.AddPolicy("PharmacyUpload", httpContext =>
            {
                var partitionKey = httpContext.User.Identity?.IsAuthenticated == true
                    ? httpContext.User.FindFirst("sub")?.Value
                      ?? httpContext.User.Identity?.Name
                      ?? httpContext.Connection.RemoteIpAddress?.ToString()
                      ?? "anonymous"
                    : httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }
}
