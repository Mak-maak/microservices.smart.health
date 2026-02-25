using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SmartHealth.Payments.Features.Payments.GetPayment;
using SmartHealth.Payments.Infrastructure.Messaging;
using SmartHealth.Payments.Infrastructure.Messaging.Consumers;
using SmartHealth.Payments.Infrastructure.Outbox;
using SmartHealth.Payments.Infrastructure.Persistence;
using SmartHealth.Payments.Infrastructure.Stripe;
using SmartHealth.Payments.Shared.Behaviours;
using System.Reflection;

// ============================================================
// Builder configuration
// ============================================================
var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------
// Azure Key Vault (production only)
// ----------------------------------------------------------
if (builder.Environment.IsProduction())
{
    var keyVaultUri = builder.Configuration["Azure:KeyVaultUri"];
    if (!string.IsNullOrWhiteSpace(keyVaultUri))
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultUri),
            new DefaultAzureCredential());
    }
}

// ----------------------------------------------------------
// Database – SQL Server via EF Core
// ----------------------------------------------------------
builder.Services.AddDbContext<PaymentsDbContext>((serviceProvider, options) =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SqlServer"),
        sql =>
        {
            sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            sql.MigrationsAssembly(typeof(PaymentsDbContext).Assembly.FullName);
        }));

// ----------------------------------------------------------
// MediatR + pipeline behaviours
// ----------------------------------------------------------
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    cfg.AddOpenBehavior(typeof(LoggingBehaviour<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
});

// ----------------------------------------------------------
// FluentValidation
// ----------------------------------------------------------
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// ----------------------------------------------------------
// JSON Serialization Options
// ----------------------------------------------------------
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

// ----------------------------------------------------------
// Stripe payment service
// ----------------------------------------------------------
builder.Services.AddSingleton<IStripePaymentService, StripePaymentService>();

// ----------------------------------------------------------
// MassTransit (event consumer + publisher)
// ----------------------------------------------------------
var useInMemoryBus = builder.Configuration.GetValue<bool>("Features:UseInMemoryBus", false);

builder.Services.AddMassTransit(x =>
{
    // Consumer for incoming AppointmentSlotReserved events
    x.AddConsumer<AppointmentSlotReservedConsumer>();

    if (useInMemoryBus)
    {
        x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
    }
    else
    {
        x.UsingAzureServiceBus((ctx, cfg) =>
        {
            cfg.Host(builder.Configuration.GetConnectionString("AzureServiceBus"));

            cfg.UseMessageRetry(r =>
                r.Exponential(5, TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));

            cfg.ConfigureEndpoints(ctx);
        });
    }
});

// ----------------------------------------------------------
// Outbox publisher background service
// ----------------------------------------------------------
builder.Services.AddHostedService<OutboxPublisherService>();

// ----------------------------------------------------------
// OpenTelemetry + Azure Application Insights
// ----------------------------------------------------------
var appInsightsConnStr = builder.Configuration["ApplicationInsights:ConnectionString"];

var otelBuilder = builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("SmartHealth.Payments"))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation()
         .AddHttpClientInstrumentation()
         .AddSource("MassTransit");

        var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            t.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
    });

if (!string.IsNullOrWhiteSpace(appInsightsConnStr))
{
    otelBuilder.UseAzureMonitor(o => o.ConnectionString = appInsightsConnStr);
}

// ----------------------------------------------------------
// Health checks
// ----------------------------------------------------------
var healthChecks = builder.Services.AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString("SqlServer") ?? string.Empty,
        name: "sql", tags: ["readiness"]);

if (!useInMemoryBus)
{
    var serviceBusConnStr = builder.Configuration.GetConnectionString("AzureServiceBus");
    if (!string.IsNullOrWhiteSpace(serviceBusConnStr))
    {
        healthChecks.AddAzureServiceBusQueue(
            serviceBusConnStr,
            "payments",
            name: "servicebus", tags: ["readiness"],
            configure: _ => { });
    }
}

// ============================================================
// Build the application
// ============================================================
var app = builder.Build();

// Auto-apply EF Core migrations on startup (dev / staging)
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    await db.Database.MigrateAsync();
}

// ============================================================
// Minimal API Endpoints
// ============================================================

// Health endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/readiness", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = hc => hc.Tags.Contains("readiness")
});
app.MapGet("/liveness", () => Results.Ok(new { status = "alive", timestamp = DateTime.UtcNow }));

// ----------------------------------------------------------
// Payments
// ----------------------------------------------------------
var payments = app.MapGroup("/api/payments").WithTags("Payments");

payments.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new GetPaymentQuery(id), ct);
    return Results.Ok(result);
})
.WithName("GetPayment")
.WithSummary("Get payment details");

// ----------------------------------------------------------
// Global error handling
// ----------------------------------------------------------
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async ctx =>
    {
        var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        ctx.Response.ContentType = "application/json";

        ctx.Response.StatusCode = ex switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            SmartHealth.Payments.Domain.Exceptions.PaymentNotFoundException => StatusCodes.Status404NotFound,
            SmartHealth.Payments.Domain.Exceptions.DuplicatePaymentException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        await ctx.Response.WriteAsJsonAsync(new
        {
            error = ex?.Message ?? "An unexpected error occurred.",
            statusCode = ctx.Response.StatusCode
        });
    });
});

await app.RunAsync();

// Make Program discoverable for integration tests
public partial class Program { }
