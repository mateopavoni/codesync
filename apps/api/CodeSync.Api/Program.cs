using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using CodeSync.Api.Auth;
using CodeSync.Application;
using CodeSync.Infrastructure;
using CodeSync.Infrastructure.Firestore.Seeding;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Logging ───────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext()
       .WriteTo.Console());

// ── Application + Infrastructure DI ──────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── Firebase Authentication ───────────────────────────────────────────────────
builder.Services
    .AddAuthentication("Firebase")
    .AddScheme<FirebaseAuthenticationOptions, FirebaseAuthenticationHandler>("Firebase", null);
builder.Services.AddAuthorization();

// ── CORS (Angular dev server + production origin) ─────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? new[] { "http://localhost:4200" };

        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ── Swagger / OpenAPI ─────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CodeSync API",
        Version = "v1",
        Description = "Backend API for the CodeSync collaborative coding platform."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "Firebase JWT",
        In = ParameterLocation.Header,
        Description = "Pegar el Firebase ID Token obtenido del cliente Angular."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// El frontend Angular manda enums como string ("python", "javascript"); sin este
// converter, System.Text.Json solo acepta los valores numéricos del enum de C#
// y cada submission llega con 400 (Language inválido).
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ── Rate limiting (protege la VPS de abuso/spam en la demo) ──────────────────
// Partición por IP: sin esto un solo cliente hambriento podía saturar el
// sandbox de Docker o Firestore. Límite global generoso + política "heavy"
// más estricta para los endpoints caros (ejecutar código, crear/unirse a sala).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));

    options.AddPolicy("heavy", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));

    options.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.ContentType = "application/problem+json";
        await ctx.HttpContext.Response.WriteAsJsonAsync(new
        {
            status = 429,
            error = "Demasiadas solicitudes. Esperá un momento y volvé a intentar.",
        }, ct);
    };
});

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Global exception handler (maps domain exceptions to correct HTTP status) ──
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var ex = feature?.Error;

        var (statusCode, error) = ex switch
        {
            KeyNotFoundException => (404, ex.Message),
            ValidationException ve => (400, string.Join("; ", ve.Errors.Select(e => e.ErrorMessage))),
            InvalidOperationException => (422, ex.Message),
            UnauthorizedAccessException => (401, "No autorizado."),
            _ => (500, "Error interno del servidor.")
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new { status = statusCode, error });
    });
});

// ── Dev: Swagger ───────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ── Challenge seed (idempotente por titulo+idioma, seguro en cualquier entorno) ─
using (var scope = app.Services.CreateScope())
{
    try
    {
        var seeder = scope.ServiceProvider.GetRequiredService<ChallengeSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Challenge seeder skipped (Firebase may not be configured).");
    }
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

app.Run();
