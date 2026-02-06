using Exodus.Models.Dto.DashboardDto;

namespace Exodus.Services.Reports;

public interface IReportService
{
    Task<SalesReportDto> GenerateSalesReportAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<InventoryReportDto> GenerateInventoryReportAsync(CancellationToken ct = default);
    Task<CustomerReportDto> GenerateCustomerReportAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
}
