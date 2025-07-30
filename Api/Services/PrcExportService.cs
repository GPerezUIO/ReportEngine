using System;
using System.Linq;
using System.Text.Json;
using Api.Data;
using Api.Dtos;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace Api.Services;

public class PrcExportService : IPcrExportService
{
    private readonly CloudPcrContext _context;

    public PrcExportService(CloudPcrContext context)
    {
        _context = context;
    }

    public async Task<byte[]> ExportPcrForAgencyAsync(long agencyId, DateTime? startDate = null, DateTime? endDate = null)
    {
        startDate ??= DateTime.Now.AddDays(-5);
        endDate ??= DateTime.Now;
        var pcrs = await _context.Pcrs.Where(p => p.TenantId == agencyId)
            .Where(p => (!startDate.HasValue || p.CreationTime >= startDate)
                && (!endDate.HasValue || p.CreationTime <= endDate)
                && p.IsDeleted == false
                && p.Pcrjson != null && p.Pcrjson.JsonData != null)
            .Select(p => new PcrDto { PcrId = p.Id, JsonData = p.Pcrjson.JsonData })
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync();

        var pcrJsonDocs = pcrs
            .Where(p => !string.IsNullOrWhiteSpace(p.JsonData))
            .Select(p => new PcrReportDto { PcrId = p.PcrId, JsonData = JsonDocument.Parse(p.JsonData) })
            .ToList();

        var ddos = await GetCollection(tenantId: agencyId, includeDeleted: false);

        List<PcrReportDto> filteredJsonDocs = FilterJsonDocs(pcrJsonDocs, DefaultAllowedProps);

        var newJsonDocs = ReplaceGuidsForDDOsValuesDynamic(filteredJsonDocs, ddos);

        var excelBytes = CreateExcelFromJsonDocuments(newJsonDocs);

        return excelBytes;
    }

    // Default allowed properties for filtering PCR JSON documents
    private static readonly HashSet<string> DefaultAllowedProps = new(new[] {
        "eResponse_03",
        "eNarrative_01",
        "eResponse_05",
        "eDisposition_17",
        "eSituation_11",
        "eSituation_12",
        "eSituation_09",
        "eSituation_10",
        "eHistory_09",
        "eExam_10",
        "eDisposition_20",
        "ePatient_07",
        "eSituation_01",
        "eVitals_23",
        "eDisposition_21",
        "eDisposition_22",
        "eResponse_23",
        "eTimes_03",
        "eTimes_05",
        "eTimes_07"
    });


    private List<PcrReportDto> FilterJsonDocs(List<PcrReportDto> jsonDocs, HashSet<string> allowedProps)
    {
        var filteredJsonDocs = jsonDocs.Select(dto =>
        {
            var filtered = new Dictionary<string, object?>();
            foreach (var prop in dto.JsonData.RootElement.EnumerateObject())
            {
                if (allowedProps.Contains(prop.Name))
                {
                    filtered[prop.Name] = prop.Value.ValueKind == JsonValueKind.Null ? null : JsonElementToObject(prop.Value);
                }
            }
            // Serialize and parse back to JsonDocument to normalize the filtered data and ensure type compatibility
            var json = JsonSerializer.Serialize(filtered);
            var pcrId = dto.PcrId;

            return new PcrReportDto
            {
                PcrId = pcrId,
                JsonData = JsonDocument.Parse(json)
            };
        }).ToList();

        return filteredJsonDocs;
    }

    // Helper to convert JsonElement to object for serialization
    private object? JsonElementToObject(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new Dictionary<string, object?>();
                foreach (var prop in element.EnumerateObject())
                {
                    obj[prop.Name] = JsonElementToObject(prop.Value);
                }
                return obj;
            case JsonValueKind.Array:
                var arr = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    arr.Add(JsonElementToObject(item));
                }
                return arr;
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Number:
                if (element.TryGetInt64(out var l)) return l;
                if (element.TryGetDouble(out var d)) return d;
                return element.GetRawText();
            case JsonValueKind.True:
            case JsonValueKind.False:
                return element.GetBoolean();
            case JsonValueKind.Null:
            default:
                return null;
        }
    }


    private List<PcrReportDto> ReplaceGuidsForDDOsValuesDynamic(List<PcrReportDto> pcrJsonDocs, List<DropdownOption> ddos)
    {
        var ddoDictionary = ddos
            .DistinctBy(ddo => ddo.Guid)
            .Where(ddo => ddo.Guid != null)
            .ToDictionary(ddo => ddo.Guid!, ddo => ddo.Value ?? string.Empty);

        var newDocs = new List<PcrReportDto>();
        foreach (var doc in pcrJsonDocs)
        {
            var root = doc.JsonData.RootElement;
            var replaced = ReplaceGuidsInJsonElement(root, ddoDictionary);
            var json = JsonSerializer.Serialize(replaced);
            newDocs.Add(new PcrReportDto
            {
                PcrId = doc.PcrId,
                JsonData = JsonDocument.Parse(json)
            });
        }
        return newDocs;
    }

    private object ReplaceGuidsInJsonElement(JsonElement element, Dictionary<string, string> ddoDictionary)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new Dictionary<string, object?>();
                foreach (var prop in element.EnumerateObject())
                {
                    obj[prop.Name] = ReplaceGuidsInJsonElement(prop.Value, ddoDictionary);
                }
                return obj;
            case JsonValueKind.Array:
                var arr = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    arr.Add(ReplaceGuidsInJsonElement(item, ddoDictionary));
                }
                return arr;
            case JsonValueKind.String:
                var str = element.GetString();
                if (!string.IsNullOrEmpty(str) && ddoDictionary.TryGetValue(str, out var ddoValue))
                    return ddoValue;
                return str ?? string.Empty;
            case JsonValueKind.Number:
                if (element.TryGetInt64(out var l)) return l;
                if (element.TryGetDouble(out var d)) return d;
                return element.GetRawText();
            case JsonValueKind.True:
            case JsonValueKind.False:
                return element.GetBoolean();
            case JsonValueKind.Null:
            default:
                return string.Empty;
        }
    }

    private byte[] CreateExcelFromJsonDocuments(List<PcrReportDto> reports)
    {
        ExcelPackage.License.SetNonCommercialPersonal("Geraldson");

        using (var package = new OfficeOpenXml.ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add("PCRs");
            if (reports == null || reports.Count == 0)
            {
                return package.GetAsByteArray();
            }

            // Collect all unique property names from all documents (flat, top-level only)
            var allProps = new HashSet<string>();
            foreach (var doc in reports)
            {
                foreach (var prop in doc.JsonData.RootElement.EnumerateObject())
                {
                    allProps.Add(prop.Name);
                }
            }
            var propList = allProps.ToList();

            // Write header
            for (int i = 0; i < propList.Count; i++)
            {
                worksheet.Cells[1, i + 1].Value = propList[i];
            }

            // Write data rows
            for (int row = 0; row < reports.Count; row++)
            {
                var doc = reports[row];
                var root = doc.JsonData.RootElement;
                for (int col = 0; col < propList.Count; col++)
                {
                    var propName = propList[col];
                    if (root.TryGetProperty(propName, out var value))
                    {
                        worksheet.Cells[row + 2, col + 1].Value = JsonElementToCellString(value);
                    }
                    else
                    {
                        worksheet.Cells[row + 2, col + 1].Value = string.Empty;
                    }
                }
            }

            return package.GetAsByteArray();
        }
    }

    private string JsonElementToCellString(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                // Flatten object as key1:val1|key2:val2
                return string.Join("|", value.EnumerateObject().Select(p => $"{p.Name}:{JsonElementToCellString(p.Value)}"));
            case JsonValueKind.Array:
                return string.Join(", ", value.EnumerateArray().Select(JsonElementToCellString));
            case JsonValueKind.String:
                return value.GetString() ?? string.Empty;
            case JsonValueKind.Number:
                return value.GetRawText();
            case JsonValueKind.True:
            case JsonValueKind.False:
                return value.GetBoolean().ToString();
            case JsonValueKind.Null:
            default:
                return string.Empty;
        }
    }

    public Task<List<DropdownOption>> GetCollection(bool includeDeleted = false, string selector = "Default", string stateCode = "Default", long? tenantId = null)
    {
        var context = _context;

        // Cargar los datos aplicando solo filtros SQL-compatibles primero
        var baseQuery = context.DropdownOptions
            .Where(x => !x.IsDeleted)
            .Where(x => x.Collection != null)
            .Where(x => x.TenantId == tenantId || x.TenantId == null)
            .Where(x => x.Selector == selector || x.Selector == stateCode)
            .AsEnumerable(); // EF ya no traduce, ahora LINQ to Objects

        // Aplicar lógica de prioridad y agrupar por Collection + Guid
        var result = baseQuery
            .GroupBy(x => new { x.Collection, x.Guid })
            .Select(g =>
                g.OrderByDescending(x => x.TenantId.HasValue)
                .ThenByDescending(x => x.Selector == selector ? 2 : (x.Selector == stateCode ? 1 : 0))
                .ThenByDescending(x => x.Id)
                .First()
            );

        // if (!includeDeleted)
        // {
        //     query = query.Where(x => x.Operation != 2);
        // }

        // Use ?? "" to avoid null issues in ordering
        return Task.FromResult(result
            .OrderBy(x => x.Collection ?? "")
            .ToList());

    }

}
