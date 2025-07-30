

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Spectre.Console;

using EpcrExportConsoleApp.Services;
using EpcrExportConsoleApp.Data;
using EpcrExportConsoleApp.Config;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.SetBasePath(Directory.GetCurrentDirectory());
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        services.Configure<PcrExportOptions>(configuration.GetSection("PcrExport"));
        services.AddDbContext<CloudPcrContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<IPcrExportService, PrcExportService>();
    })
    .Build();

using (host)
{
    await host.StartAsync();
    var provider = host.Services;

    // Fancy header
    AnsiConsole.Write(new FigletText("PCR Report Exporter").Centered().Color(new Color(0, 255, 255))); // cyan
    AnsiConsole.Write(new Rule("[yellow]Welcome![/]").RuleStyle("grey").Centered());
    AnsiConsole.Write(new Panel("[bold]This tool exports PCR reports for a given agency and date range.[/]").Border(BoxBorder.Rounded).BorderStyle(new Style(new Color(0, 0, 255))).Padding(1,1)); // blue

    // Prompt for Agency ID (as long), Start Date, End Date
    var agencyId = AnsiConsole.Prompt(
        new TextPrompt<long>("[green]Enter Agency ID (number):[/]")
            .PromptStyle("yellow")
            .Validate(id => id > 0 ? ValidationResult.Success() : ValidationResult.Error("[red]Invalid Agency ID[/]")));

    var startDate = AnsiConsole.Prompt(
        new TextPrompt<DateTime>("[green]Enter Start Date (yyyy-MM-dd):[/]")
            .PromptStyle("yellow")
            .Validate(dt => dt != default ? ValidationResult.Success() : ValidationResult.Error("[red]Invalid date[/]")));

    var endDate = AnsiConsole.Prompt(
        new TextPrompt<DateTime>("[green]Enter End Date (yyyy-MM-dd):[/]")
            .PromptStyle("yellow")
            .Validate(dt => dt != default ? ValidationResult.Success() : ValidationResult.Error("[red]Invalid date[/]")));

    AnsiConsole.Write(new Rule("[blue]Generating Report[/]").RuleStyle("grey").Centered());

    // Progress bar for report generation
    await AnsiConsole.Progress()
        .StartAsync(async ctx =>
        {
            var task = ctx.AddTask("[yellow]Exporting report...[/]");
            task.IsIndeterminate = true;
            await ReportOrchestrator.RunAsync(provider, agencyId, startDate, endDate);
            task.StopTask();
        });

    AnsiConsole.Write(new Rule("[green]Done[/]").RuleStyle("grey").Centered());

    await host.StopAsync();
}

public static class ReportOrchestrator
{
    public static async Task RunAsync(IServiceProvider provider, long agencyId, DateTime startDate, DateTime endDate)
    {
        var exportService = provider.GetRequiredService<IPcrExportService>();
        try
        {
            var result = await exportService.ExportPcrForAgencyAsync(agencyId, startDate, endDate);
            var fileName = $"report_{agencyId}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            await File.WriteAllBytesAsync(fileName, result);
            AnsiConsole.Write(new Panel($"[green]✅ Report generated successfully![/]\n[bold]Saved as:[/] {fileName}").Border(BoxBorder.Double).BorderStyle(new Style(new Color(0, 255, 0))).Padding(1,1)); // green
        }
        catch (Exception ex)
        {
            AnsiConsole.Write(new Panel($"[red]❌ Error:[/] {ex.Message}").Border(BoxBorder.Double).BorderStyle(new Style(new Color(255, 0, 0))).Padding(1,1)); // red
        }
    }
}
