using Api.Data;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<IPcrExportService, PrcExportService>();
builder.Services.AddDbContext<CloudPcrContext>();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapGet("/pcr/{agencyId}", async (
    [FromServices] IPcrExportService pcrExportService,
    long agencyId,
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate) =>
{
    var pcrs = await pcrExportService.ExportPcrForAgencyAsync(agencyId, startDate, endDate);
    var reportName = $"PCR_Report_{agencyId}_From_{startDate:yyyyMMdd}_To_{endDate:yyyyMMdd}.xlsx";
    return Results.File(pcrs, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", reportName);
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
