using System;

namespace EpcrExportConsoleApp.Services;

public interface IPcrExportService
{
    Task<byte[]> ExportPcrForAgencyAsync(long agencyId, DateTime? startDate = null, DateTime? endDate = null);
}
