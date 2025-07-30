using System;
using System.Text.Json;

namespace EpcrExportConsoleApp.Dtos;

public class PcrReportDto
{
    public long PcrId { get; set; }
    public JsonDocument JsonData { get; set; }
}
