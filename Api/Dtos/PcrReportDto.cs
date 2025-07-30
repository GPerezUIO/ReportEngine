using System;
using System.Text.Json;

namespace Api.Dtos;

public class PcrReportDto
{
    public long PcrId { get; set; }
    public JsonDocument JsonData { get; set; }
}
