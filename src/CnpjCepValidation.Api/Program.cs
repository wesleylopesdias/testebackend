using System.Text.Json;
using System.Text.Json.Serialization;
using CnpjCepValidation.Api.Middleware;
using CnpjCepValidation.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

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
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
