using Api.Data;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<IPcrExportService, PrcExportService>();
builder.Services.AddDbContext<CloudPcrContext>();



var app = builder.Build();

// Configure the HTTP request pipeline.

// Enable Scalar UI for API testing
app.MapOpenApi();
app.MapScalarApiReference(options =>
    {
        options.WithTitle("My API");
        options.WithTheme(ScalarTheme.BluePlanet);
    });

app.UseHttpsRedirection();


app.MapGet("/pcr/{agencyId}", async (
    [FromServices] IPcrExportService pcrExportService,
    long agencyId,
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate) =>
{
    var pcrs = await pcrExportService.ExportPcrForAgencyAsync(agencyId, startDate, endDate);
    var reportName = $"PCR_Report_{agencyId}_From_{startDate:yyyyMMdd}_To_{endDate:yyyyMMdd}.xlsx";
    return Results.File(pcrs, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", reportName);
})
.Produces<byte[]>(StatusCodes.Status200OK, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
  .Produces(StatusCodes.Status404NotFound)
  .Produces(StatusCodes.Status500InternalServerError)
  .WithName("ExportPcrForAgency")
  .WithTags("PCR Export")
  .WithSummary("Export PCRs for a specific agency")
    .WithDescription("Exports PCRs for a specific agency within a date range. " +
                     "If no dates are provided, it defaults to the last 30 days.");


app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
