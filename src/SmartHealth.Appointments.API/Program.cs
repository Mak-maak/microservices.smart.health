using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SmartHealth.Appointments.Features.Appointments.BookAppointment;
using SmartHealth.Appointments.Features.Appointments.CancelAppointment;
using SmartHealth.Appointments.Features.Appointments.GetAppointment;
using SmartHealth.Appointments.Features.Doctors;
using SmartHealth.Appointments.Features.Patients;
using SmartHealth.Appointments.Infrastructure.Caching;
using SmartHealth.Appointments.Infrastructure.EventSourcing;
using SmartHealth.Appointments.Infrastructure.Messaging;
using SmartHealth.Appointments.Infrastructure.Outbox;
using SmartHealth.Appointments.Infrastructure.Persistence;
using SmartHealth.Appointments.Infrastructure.Saga;
using SmartHealth.Appointments.Shared.Behaviours;
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
// Database - Azure SQL via EF Core
// ----------------------------------------------------------
builder.Services.AddDbContext<AppointmentsDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SqlServer"),
        sql =>
        {
            sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            sql.MigrationsAssembly(typeof(AppointmentsDbContext).Assembly.FullName);
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
// MassTransit (orchestration saga + choreography consumers)
// ----------------------------------------------------------
var useSagaOrchestration = builder.Configuration.GetValue<bool>("Features:SagaOrchestration", true);
var useChoreography = builder.Configuration.GetValue<bool>("Features:Choreography", false);
var useInMemoryBus = builder.Configuration.GetValue<bool>("Features:UseInMemoryBus", false);

builder.Services.AddMassTransit(x =>
{
    // Saga state machine (orchestration-based)
    if (useSagaOrchestration)
    {
        x.AddSagaStateMachine<AppointmentBookingSaga, AppointmentSagaState>()
            .EntityFrameworkRepository(r =>
            {
                r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                r.AddDbContext<DbContext, AppointmentsDbContext>((sp, o) =>
                    o.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")!));
            });
    }

    // Choreography consumers (alternative)
    if (useChoreography)
    {
        x.AddConsumer<AppointmentRequestedConsumer>();
        x.AddConsumer<ConfirmAppointmentConsumer>();
        x.AddConsumer<CompensateAppointmentConsumer>();
    }

    if (useInMemoryBus)
    {
        // In-memory transport for local dev / testing
        x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
    }
    else
    {
        // Azure Service Bus transport
        x.UsingAzureServiceBus((ctx, cfg) =>
        {
            cfg.Host(builder.Configuration.GetConnectionString("AzureServiceBus"));

            // Dead-letter + retry configuration
            cfg.UseMessageRetry(r =>
                r.Exponential(5, TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));

            cfg.ConfigureEndpoints(ctx);
        });
    }
});

// ----------------------------------------------------------
// Redis Cache (Azure Cache for Redis)
// ----------------------------------------------------------
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "SmartHealth:";
});
builder.Services.AddSingleton<RedisCacheService>();

// ----------------------------------------------------------
// Event Sourcing (optional feature flag)
// ----------------------------------------------------------
if (builder.Configuration.GetValue<bool>("Features:EventSourcing"))
{
    builder.Services.AddScoped<EventStoreService>();
}

// ----------------------------------------------------------
// Outbox publisher background service
// ----------------------------------------------------------
builder.Services.AddHostedService<OutboxPublisherService>();

// ----------------------------------------------------------
// OpenTelemetry + Azure Application Insights
// ----------------------------------------------------------
var appInsightsConnStr = builder.Configuration["ApplicationInsights:ConnectionString"];

var otelBuilder = builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("SmartHealth.Appointments"))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation()
         .AddHttpClientInstrumentation()
         .AddSource("MassTransit");

        var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            t.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
    });

// Azure Application Insights - chain UseAzureMonitor onto the OTel builder
if (!string.IsNullOrWhiteSpace(appInsightsConnStr))
{
    otelBuilder.UseAzureMonitor(o => o.ConnectionString = appInsightsConnStr);
}

// ----------------------------------------------------------
// Health checks
// ----------------------------------------------------------
builder.Services
    .AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString("SqlServer") ?? string.Empty,
        name: "sql", tags: ["readiness"])
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis") ?? string.Empty,
        name: "redis", tags: ["readiness"])
    .AddAzureServiceBusQueue(
        builder.Configuration.GetConnectionString("AzureServiceBus") ?? string.Empty,
        "appointments",
        name: "servicebus", tags: ["readiness"],
        configure: _ => { });

// ============================================================
// Build the application
// ============================================================
var app = builder.Build();

// Auto-apply EF Core migrations on startup (dev / staging)
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppointmentsDbContext>();
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
// Appointments
// ----------------------------------------------------------
var appointments = app.MapGroup("/api/appointments").WithTags("Appointments");

appointments.MapPost("/", async (BookAppointmentCommand command, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(command, ct);
    return Results.Created($"/api/appointments/{result.AppointmentId}", result);
})
.WithName("BookAppointment")
.WithSummary("Book a new appointment");

appointments.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new GetAppointmentQuery(id), ct);
    return Results.Ok(result);
})
.WithName("GetAppointment")
.WithSummary("Get appointment details");

appointments.MapDelete("/{id:guid}", async (Guid id, string reason, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new CancelAppointmentCommand(id, reason), ct);
    return Results.Ok(result);
})
.WithName("CancelAppointment")
.WithSummary("Cancel an appointment");

// ----------------------------------------------------------
// Patients
// ----------------------------------------------------------
var patients = app.MapGroup("/api/patients").WithTags("Patients");

patients.MapPost("/", async (CreatePatientCommand command, IMediator mediator, CancellationToken ct) =>
{
    var id = await mediator.Send(command, ct);
    return Results.Created($"/api/patients/{id}", new { id });
})
.WithName("CreatePatient");

patients.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new GetPatientQuery(id), ct);
    return result is null ? Results.NotFound() : Results.Ok(result);
})
.WithName("GetPatient");

// ----------------------------------------------------------
// Doctors
// ----------------------------------------------------------
var doctors = app.MapGroup("/api/doctors").WithTags("Doctors");

doctors.MapPost("/", async (CreateDoctorCommand command, IMediator mediator, CancellationToken ct) =>
{
    var id = await mediator.Send(command, ct);
    return Results.Created($"/api/doctors/{id}", new { id });
})
.WithName("CreateDoctor");

doctors.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new GetDoctorQuery(id), ct);
    return result is null ? Results.NotFound() : Results.Ok(result);
})
.WithName("GetDoctor");

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
            SmartHealth.Appointments.Domain.Exceptions.AppointmentNotFoundException => StatusCodes.Status404NotFound,
            SmartHealth.Appointments.Domain.Exceptions.PatientNotFoundException => StatusCodes.Status404NotFound,
            SmartHealth.Appointments.Domain.Exceptions.DoctorNotFoundException => StatusCodes.Status404NotFound,
            SmartHealth.Appointments.Domain.Exceptions.DoctorDoubleBookingException => StatusCodes.Status409Conflict,
            SmartHealth.Appointments.Domain.Exceptions.AppointmentInPastException => StatusCodes.Status422UnprocessableEntity,
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
