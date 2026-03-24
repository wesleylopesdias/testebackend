using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using CnpjCepValidation.Api.Middleware;
using CnpjCepValidation.Application.Diagnostics;
using CnpjCepValidation.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new() { Title = "CNPJ/CEP Validation API", Version = "v1" }));

builder.Services.Configure<ApiBehaviorOptions>(opts =>
    opts.SuppressModelStateInvalidFilter = false);

// API Versioning
builder.Services.AddApiVersioning(opts =>
{
    opts.DefaultApiVersion = new ApiVersion(1, 0);
    opts.AssumeDefaultVersionWhenUnspecified = true;
    opts.ReportApiVersions = true;
    opts.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer(opts =>
{
    opts.GroupNameFormat = "'v'VVV";
    opts.SubstituteApiVersionInUrl = true;
});

// Rate Limiting
builder.Services.AddRateLimiter(opts =>
{
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opts.AddFixedWindowLimiter("fixed", limiter =>
    {
        limiter.PermitLimit = 100;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 10;
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddUrlGroup(
        new Uri(builder.Configuration["ExternalApis:BrasilApiBaseUrl"] + "/api/cep/v2/01001000"),
        name: "brasilapi",
        tags: ["readiness"])
    .AddUrlGroup(
        new Uri(builder.Configuration["ExternalApis:ViaCepBaseUrl"] + "/ws/01001000/json/"),
        name: "viacep",
        tags: ["readiness"]);

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(res => res.AddService("CnpjCepValidation"))
    .WithTracing(tracing => tracing
        .AddSource(ValidationDiagnostics.ActivitySourceName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddMeter(ValidationDiagnostics.MeterName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "application/problem+json";

    var pd = new ProblemDetails
    {
        Status = StatusCodes.Status500InternalServerError,
        Title = "Erro interno do servidor.",
        Detail = "Ocorreu um erro inesperado. Tente novamente mais tarde."
    };

    if (context.Items.TryGetValue("x-correlation-id", out var cid))
        pd.Extensions["correlationId"] = cid?.ToString();

    await context.Response.WriteAsJsonAsync(pd);
}));

app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("readiness")
});

app.MapControllers();

app.Run();

public partial class Program { }
