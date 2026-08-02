using FinancialTransaction.Api.Endpoints;
using FinancialTransaction.Api.ExceptionHandling;
using FinancialTransaction.Application;
using FinancialTransaction.Infrastructure;
using FinancialTransaction.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "FinancialTransaction API v1");
        options.RoutePrefix = "swagger";
    });

    await AccountsSeeder.SeedAsync(app.Services, app.Logger);
}

app.UseHttpsRedirection();

app.MapAccountEndpoints();
app.MapTransactionEndpoints();

app.Run();

public partial class Program
{
}
