using System.Text;
using DistriFlow.Api.Middleware;
using DistriFlow.Api.Security;
using DistriFlow.Application;
using DistriFlow.Application.Common.Security;
using DistriFlow.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── CORS ──────────────────────────────────────────────────────────────────────
const string FrontendPolicy = "FrontendPolicy";
var allowedOrigins = GetAllowedOrigins(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendPolicy, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ── Servicios ─────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("La configuración Jwt es requerida.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            RoleClaimType = DistriFlowClaims.Role
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AuthorizationPolicies.ActiveUser,
        policy => policy
            .RequireAuthenticatedUser()
            .AddRequirements(new SessionAccessRequirement(false)));

    options.AddPolicy(
        AuthorizationPolicies.ActiveDistributor,
        policy => policy
            .RequireAuthenticatedUser()
            .AddRequirements(new SessionAccessRequirement(true)));
});
builder.Services.AddScoped<IAuthorizationHandler, SessionAccessAuthorizationHandler>();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1",
            Title = "Error de validación",
            Status = StatusCodes.Status400BadRequest,
            Detail = "La solicitud contiene datos inválidos.",
            Instance = context.HttpContext.Request.Path
        };
        problemDetails.Extensions["code"] = "validation.invalid_request";
        problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        return new BadRequestObjectResult(problemDetails);
    };
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── Pipeline ──────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();


if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseCors(FrontendPolicy);        // ← nombre explícito
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// ── Helpers ───────────────────────────────────────────────────────────────────
static string[] GetAllowedOrigins(IConfiguration configuration)
{
    var configured = configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    var origins = configured
        .Where(o => !string.IsNullOrWhiteSpace(o) && o != "*")
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    return origins.Length > 0
        ? origins
        : throw new InvalidOperationException(
            "Cors:AllowedOrigins debe contener al menos un origen permitido.");
}
